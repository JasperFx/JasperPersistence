#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JasperFx.Core.Reflection;
using Marten.Events.Daemon;
using Marten.Events.Daemon.Internals;
using Marten.Events.Projections;
using Marten.Exceptions;
using Marten.Internal.Operations;
using Marten.Internal.Sessions;
using Marten.Internal.Storage;
using Marten.Services;
using Marten.Sessions;
using Marten.Storage;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Marten.Events.Aggregation;

public abstract class CrossStreamAggregationRuntime<TDoc, TId>: AggregationRuntime<TDoc, TId>
    where TDoc : notnull where TId : notnull
{
    public CrossStreamAggregationRuntime(IDocumentStore store, IAggregateProjection projection,
        IEventSlicer<TDoc, TId> slicer, IDocumentStorage<TDoc, TId> storage): base(store, projection, slicer, storage)
    {
    }

    public override bool IsNew(EventSlice<TDoc, TId> slice)
    {
        return false;
    }
}

/// <summary>
///     Internal base class for runtime event aggregation
/// </summary>
/// <typeparam name="TDoc"></typeparam>
/// <typeparam name="TId"></typeparam>
public abstract class AggregationRuntime<TDoc, TId>: IAggregationRuntime<TDoc, TId>
    where TDoc : notnull where TId : notnull
{
    public AggregationRuntime(IDocumentStore store, IAggregateProjection projection, IEventSlicer<TDoc, TId> slicer,
        IDocumentStorage<TDoc, TId> storage)
    {
        Projection = projection;
        Slicer = slicer;
        Storage = storage;
        Options = store.As<DocumentStore>().Options;
    }

    internal StoreOptions Options { get; }

    public IAggregateProjection Projection { get; }
    public IEventSlicer<TDoc, TId> Slicer { get; }
    public IDocumentStorage<TDoc, TId> Storage { get; }

    public async ValueTask ApplyChangesAsync(DocumentSessionBase session,
        EventSlice<TDoc, TId> slice, CancellationToken cancellation,
        ProjectionLifecycle lifecycle = ProjectionLifecycle.Inline)
    {
        session = session.UseTenancyBasedOnSliceAndStorage(Storage, slice);

        if (Projection.MatchesAnyDeleteType(slice))
        {
            var operation = Storage.DeleteForId(slice.Id, slice.Tenant.TenantId);

            await processPossibleSideEffects(session, slice).ConfigureAwait(false);

            session.QueueOperation(operation);
            return;
        }

        var aggregate = slice.Aggregate;
        // do not load if sliced by stream and the stream does not yet exist
        if (slice.Aggregate == null && lifecycle == ProjectionLifecycle.Inline && (Slicer is not ISingleStreamSlicer || slice.ActionType != StreamActionType.Start))
        {
            if (session.Options.Events.UseIdentityMapForInlineAggregates)
            {
                // It's actually important to go in through the front door and use the session so that
                // the identity map can kick in here
                aggregate = await session.LoadAsync<TDoc>(slice.Id, cancellation).ConfigureAwait(false);
            }
            else
            {
                aggregate = await Storage.LoadAsync(slice.Id, session, cancellation).ConfigureAwait(false);
            }
        }

        // Does the aggregate already exist before the events are applied?
        var exists = aggregate != null;

        foreach (var @event in slice.Events())
        {
            try
            {
                aggregate = await ApplyEvent(session, slice, @event, aggregate, cancellation).ConfigureAwait(false);
            }
            catch (MartenCommandException)
            {
                throw;
            }
            catch (NpgsqlException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ApplyEventException(@event, e);
            }
        }

        if (aggregate != null)
        {
            Storage.SetIdentity(aggregate, slice.Id);
        }

        if (session is ProjectionDocumentSession pds && pds.Mode == ShardExecutionMode.Continuous)
        {
            // Need to set the aggregate in case it didn't exist upfront
            slice.Aggregate = aggregate;
            await processPossibleSideEffects(session, slice).ConfigureAwait(false);
        }

        var lastEvent = slice.Events().LastOrDefault();
        if (aggregate != null)
        {
            Versioning.TrySetVersion(aggregate, lastEvent);

            Projection.ApplyMetadata(aggregate, lastEvent);
        }

        // Delete the aggregate *if* it existed prior to these events
        if (aggregate == null)
        {
            if (exists)
            {
                var operation = Storage.DeleteForId(slice.Id, slice.Tenant.TenantId);
                session.QueueOperation(operation);
            }

            return;
        }

        var storageOperation = Storage.Upsert(aggregate, session, slice.Tenant.TenantId);
        if (Slicer is ISingleStreamSlicer && lastEvent != null && storageOperation is IRevisionedOperation op)
        {
            op.Revision = (int)lastEvent.Version;
            op.IgnoreConcurrencyViolation = true;
        }

        session.QueueOperation(storageOperation);
    }

    private async Task processPossibleSideEffects(DocumentSessionBase session, EventSlice<TDoc, TId> slice)
    {
        if (Projection is IAggregateProjectionWithSideEffects<TDoc> sideEffects)
        {
            await sideEffects.RaiseSideEffects(session, slice).ConfigureAwait(false);

            if (slice.RaisedEvents != null)
            {
                var storage = session.EventStorage();
                var ops = slice.BuildOperations(Options.EventGraph, session, storage, sideEffects.IsSingleStream());
                foreach (var op in ops)
                {
                    session.QueueOperation(op);
                }
            }

            if (slice.PublishedMessages != null)
            {
                var batch = await session.CurrentMessageBatch().ConfigureAwait(false);
                foreach (var message in slice.PublishedMessages)
                {
                    await batch.PublishAsync(message).ConfigureAwait(false);
                }
            }
        }
    }

    public IAggregateVersioning Versioning { get; set; }


    public virtual bool IsNew(EventSlice<TDoc, TId> slice)
    {
        return slice.Events().First().Version == 1;
    }

    public void Apply(IDocumentOperations operations, IReadOnlyList<StreamAction> streams)
    {
#pragma warning disable VSTHRD002
        ApplyAsync(operations, streams, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
    }

    public async Task ApplyAsync(IDocumentOperations operations, IReadOnlyList<StreamAction> streams,
        CancellationToken cancellation)
    {
        // Doing the filtering here to prevent unnecessary network round trips by allowing
        // an aggregate projection to "work" on a stream with no matching events
        var filteredStreams = streams
            .Where(x => Projection.AppliesTo(x.Events.Select(x => x.EventType)))
            .ToArray();

        var slices = await Slicer.SliceInlineActions(operations, filteredStreams).ConfigureAwait(false);

        var martenSession = (DocumentSessionBase)operations;

        await martenSession.Database.EnsureStorageExistsAsync(typeof(TDoc), cancellation).ConfigureAwait(false);

        foreach (var slice in slices)
        {
            await ApplyChangesAsync(martenSession, slice, cancellation).ConfigureAwait(false);
        }
    }

    public async ValueTask<EventRangeGroup> GroupEvents(DocumentStore store, IMartenDatabase database, EventRange range,
        CancellationToken cancellationToken)
    {
        await using var session = store.LightweightSession(SessionOptions.ForDatabase(database));
        var groups = await Slicer.SliceAsyncEvents(session, range.Events).ConfigureAwait(false);

        return new TenantSliceRange<TDoc, TId>(store, this, range, groups, cancellationToken);
    }

    public abstract ValueTask<TDoc> ApplyEvent(IQuerySession session, EventSlice<TDoc, TId> slice,
        IEvent evt, TDoc? aggregate,
        CancellationToken cancellationToken);

    public TDoc CreateDefault(IEvent @event)
    {
        try
        {
            return (TDoc)Activator.CreateInstance(typeof(TDoc), true);
        }
        catch (Exception e)
        {
            throw new System.InvalidOperationException($"There is no default constructor for {typeof(TDoc).FullNameInCode()} or Create method for {@event.DotNetTypeName} event type.Check more about the create method convention in documentation: https://martendb.io/events/projections/event-projections.html#create-method-convention. If you're using Upcasting, check if {@event.DotNetTypeName} is an old event type. If it is, make sure to define transformation for it to new event type. Read more in Upcasting docs: https://martendb.io/events/versioning.html#upcasting-advanced-payload-transformations.");
        }
    }
}
