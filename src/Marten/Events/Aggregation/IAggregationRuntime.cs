using System.Threading;
using System.Threading.Tasks;
using Marten.Events.Daemon;
using Marten.Events.Daemon.Internals;
using Marten.Events.Projections;
using Marten.Internal.Sessions;
using Marten.Internal.Storage;
using Marten.Storage;
using Marten.Util;

namespace Marten.Events.Aggregation;

/// <summary>
///     Internal interface for runtime event aggregation
/// </summary>
public interface IAggregationRuntime: IProjection
{
    IAggregateVersioning Versioning { get; set; }

    ValueTask<EventRangeGroup> GroupEvents(DocumentStore store, IMartenDatabase database, EventRange range,
        CancellationToken cancellationToken);

    bool TryBuildReplayExecutor(DocumentStore store, IMartenDatabase database, out IReplayExecutor executor);

    IAggregateProjection Projection { get; }
}

public interface IAggregationRuntime<TDoc, TId>: IAggregationRuntime where TDoc : notnull where TId : notnull
{
    IDocumentStorage<TDoc, TId> Storage { get; }

    IEventSlicer<TDoc, TId> Slicer { get; }


    ValueTask ApplyChangesAsync(DocumentSessionBase session,
        EventSlice<TDoc, TId> slice, CancellationToken cancellation,
        ProjectionLifecycle lifecycle = ProjectionLifecycle.Inline);

    bool IsNew(EventSlice<TDoc, TId> slice);

    IAggregateCache<TId, TDoc> CacheFor(Tenant tenant);

    TId IdentityFromEvent(IEvent e);
}
