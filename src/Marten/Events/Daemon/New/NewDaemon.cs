using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JasperFx.Core;
using JasperFx.Core.Reflection;
using Marten.Events.Daemon.HighWater;
using Marten.Events.Daemon.Progress;
using Marten.Events.Projections;
using Marten.Services;
using Marten.Storage;
using Microsoft.Extensions.Logging;

namespace Marten.Events.Daemon.New;

/*
 * TODO
 * New SubscriptionAgent needs to broadcast through ShardStateTracker
 * IShardFactory
 * Add logging to SubscriptionAgent
 * Implement real IDeadLetterQueue
 * SubscriptionAgent.StopAndDrainAsync()
 */


public interface IDeadLetterEventQueue
{
    void Enqueue(DeadLetterEvent dlEvent);
}

public class NewDaemon : IProjectionDaemon, IObserver<ShardState>
{
    private readonly DocumentStore _store;
    private readonly IAgentFactory _factory;
    private readonly List<ISubscriptionAgent> _active = new();
    private CancellationTokenSource _cancellation = new();
    private readonly HighWaterAgent _highWater;

    public NewDaemon(DocumentStore store, MartenDatabase database, ILogger logger, IHighWaterDetector detector, IAgentFactory factory, ShardStateTracker tracker)
    {
        Database = database;
        _store = store;
        _factory = factory;
        Logger = logger;
        Tracker = tracker;
        _highWater = new HighWaterAgent(detector, Tracker, logger, store.Options.Projections, _cancellation.Token);

    }

    public MartenDatabase Database { get; }

    public ILogger Logger { get; }

    public void Dispose()
    {
        _cancellation?.Dispose();
        _highWater?.Dispose();
    }

    public ShardStateTracker Tracker { get; }
    public bool IsRunning => _highWater.IsRunning;
    public Task RebuildProjection(string projectionName, CancellationToken token)
    {
        return RebuildProjection(projectionName, 5.Minutes(), token);
    }

    public Task RebuildProjection<TView>(CancellationToken token)
    {
        return RebuildProjection<TView>(5.Minutes(), token);
    }

    public Task RebuildProjection(Type projectionType, CancellationToken token)
    {
        return RebuildProjection(projectionType, 5.Minutes(), token);
    }

    public Task RebuildProjection(Type projectionType, TimeSpan shardTimeout, CancellationToken token)
    {
        if (projectionType.CanBeCastTo<IProjection>())
        {
            var projectionName = projectionType.FullNameInCode();
            return RebuildProjection(projectionName, shardTimeout, token);
        }

        if (projectionType.CanBeCastTo<IProjectionSource>())
        {
            try
            {
                var projection = Activator.CreateInstance(projectionType);
                if (projection is IProjectionSource wrapper)
                    return RebuildProjection(wrapper.ProjectionName, shardTimeout, token);

                throw new ArgumentOutOfRangeException(nameof(projectionType),
                    $"Type {projectionType.FullNameInCode()} is not a valid projection type");
            }
            catch (Exception e)
            {
                throw new ArgumentOutOfRangeException(nameof(projectionType),
                    $"No public default constructor for projection type {projectionType.FullNameInCode()}, you may need to supply the projection name instead");
            }
        }

        // Assume this is an aggregate type name
        return RebuildProjection(projectionType.NameInCode(), shardTimeout, token);
    }

    public Task RebuildProjection(string projectionName, TimeSpan shardTimeout, CancellationToken token)
    {
        if (!_store.Options.Projections.TryFindProjection(projectionName, out var projection))
        {
            throw new ArgumentOutOfRangeException(nameof(projectionName),
                $"No registered projection matches the name '{projectionName}'. Available names are {_store.Options.Projections.AllProjectionNames().Join(", ")}");
        }

        return rebuildProjection(projection, shardTimeout, token);
    }

    public Task RebuildProjection<TView>(TimeSpan shardTimeout, CancellationToken token)
    {
        if (typeof(TView).CanBeCastTo(typeof(ProjectionBase)) && typeof(TView).HasDefaultConstructor())
        {
            var projection = (ProjectionBase)Activator.CreateInstance(typeof(TView))!;
            return RebuildProjection(projection.ProjectionName, shardTimeout, token);
        }

        return RebuildProjection(typeof(TView).Name, shardTimeout, token);
    }

    private async Task startAgent(ISubscriptionAgent agent, ShardExecutionMode mode)
    {
        // TODO -- harden this
        var position = await Database.ProjectionProgressFor(agent.Name, _cancellation.Token).ConfigureAwait(false);
        agent.Start(position, mode, Tracker);
        _active.Add(agent);
    }

    public async Task StartShard(string shardName, CancellationToken token)
    {
        var agent = _active.FirstOrDefault(x => x.Name.Identity == shardName);
        if (agent != null) return;

        agent = _factory.BuildAgentForShard(shardName, Database);
        await startAgent(agent, ShardExecutionMode.Continuous).ConfigureAwait(false);
    }

    public async Task StopShard(string shardName, Exception ex = null)
    {
        // TODO -- harden this
        var agent = _active.FirstOrDefault(x => x.Name.Identity == shardName);
        if (agent != null)
        {
            // TODO -- use a timeout here
            await agent.StopAndDrainAsync(CancellationToken.None).ConfigureAwait(true);
            _active.Remove(agent);
        }
    }

    public async Task StartAllShards()
    {
        var agents = _factory.BuildAllAgents(Database);
        foreach (var agent in agents)
        {
            await startAgent(agent, ShardExecutionMode.Continuous).ConfigureAwait(false);
        }
    }

    public async Task StopAll()
    {
        // TODO -- use a timeout. And make sure that you harden within the SubscriptionAgent
        await Parallel.ForEachAsync(_active, CancellationToken.None, (agent, t) => new ValueTask(agent.StopAndDrainAsync(t))).ConfigureAwait(false);

        _active.Clear();
    }

    public async Task StartDaemon()
    {
        await Database.EnsureStorageExistsAsync(typeof(IEvent), _cancellation.Token).ConfigureAwait(false);
        await _highWater.Start().ConfigureAwait(false);
    }

    public async Task WaitForNonStaleData(TimeSpan timeout)
    {
        var stopWatch = Stopwatch.StartNew();
        var statistics = await Database.FetchEventStoreStatistics(_cancellation.Token).ConfigureAwait(false);

        while (stopWatch.Elapsed < timeout)
        {
            if (_active.All(x => x.Position >= statistics.EventSequenceNumber))
            {
                return;
            }

            await Task.Delay(100.Milliseconds(), _cancellation.Token).ConfigureAwait(false);
        }

        var message = $"The active projection shards did not reach sequence {statistics.EventSequenceNumber} in time";
        throw new TimeoutException(message);
    }

    public Task PauseHighWaterAgent()
    {
        return _highWater.Stop();
    }

    public long HighWaterMark()
    {
        return Tracker.HighWaterMark;
    }

    void IObserver<ShardState>.OnCompleted()
    {
        // Nothing
    }

    void IObserver<ShardState>.OnError(Exception error)
    {
        // Nothing
    }

    void IObserver<ShardState>.OnNext(ShardState value)
    {
        if (value.ShardName == ShardState.HighWaterMark)
        {
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug("Event high water mark detected at {Sequence}", value.Sequence);
            }

            foreach (var agent in _active)
            {
                agent.MarkHighWater(value.Sequence);
            }
        }
    }

    // TODO -- ZOMG, this is awful
    private async Task rebuildProjection(IProjectionSource source, TimeSpan shardTimeout, CancellationToken token)
    {
        await Database.EnsureStorageExistsAsync(typeof(IEvent), token).ConfigureAwait(false);

        Logger.LogInformation("Starting to rebuild Projection {ProjectionName}@{DatabaseIdentifier}",
            source.ProjectionName, Database.Identifier);

        var running = _active.Where(x => x.Name.ProjectionName == source.ProjectionName).ToArray();
        foreach (var agent in running)
        {
            await agent.HardStopAsync().ConfigureAwait(false);
            _active.Remove(agent);
        }

        if (token.IsCancellationRequested)
        {
            return;
        }

        await _highWater.CheckNow().ConfigureAwait(false);

        // If there's no data, do nothing
        if (Tracker.HighWaterMark == 0)
        {
            Logger.LogInformation("Aborting projection rebuild because the high water mark is 0 (no event data)");
            return;
        }

        if (token.IsCancellationRequested)
        {
            return;
        }

        var agents = _factory.BuildAgentsForProjection(source.ProjectionName, Database);

        foreach (var agent in agents)
        {
            Tracker.MarkAsRestarted(agent.Name);
        }

        // Teardown the current state
        await teardownExistingProjectionProgress(source, token, agents).ConfigureAwait(false);

        if (token.IsCancellationRequested)
        {
            return;
        }

        var mark = Tracker.HighWaterMark;

        // Is the shard count the optimal DoP here?
        await Parallel.ForEachAsync(agents,
            new ParallelOptions { CancellationToken = token, MaxDegreeOfParallelism = agents.Count },
            async (agent, cancellationToken) =>
            {
                Tracker.MarkAsRestarted(agent.Name);

                await startAgent(agent, ShardExecutionMode.Rebuild).ConfigureAwait(false);

                await Tracker.WaitForShardState(agent.Name, mark, shardTimeout).ConfigureAwait(false);
            }).ConfigureAwait(false);

        foreach (var agent in agents)
        {
            // TODO -- timeout and harden here
            await agent.StopAndDrainAsync(CancellationToken.None).ConfigureAwait(false);
        }


        // foreach (var shard in shards)
        // {
            // TODO -- find all serialization errors and write out
            // if (_agents.TryGetValue(shard.Name.Identity, out var agent))
            // {
            //     var serializationFailures = await agent.DrainSerializationFailureRecording().ConfigureAwait(false);
            //     if (serializationFailures > 0)
            //     {
            //         Console.WriteLine(
            //             $"There were {serializationFailures} deserialization failures during rebuild of shard {shard.Name.Identity}");
            //     }
            // }

            // await StopShard(shard.Name.Identity).ConfigureAwait(false);
        // }
    }


    private async Task teardownExistingProjectionProgress(IProjectionSource source, CancellationToken token,
        IReadOnlyList<ISubscriptionAgent> agents)
    {
        var sessionOptions = SessionOptions.ForDatabase(Database);
        sessionOptions.AllowAnyTenant = true;
        await using var session = _store.LightweightSession(sessionOptions);
        source.Options.Teardown(session);

        foreach (var agent in agents)
        {
            session.QueueOperation(new DeleteProjectionProgress(_store.Events, agent.Name.Identity));
        }

        // Rewind previous DeadLetterEvents because you're going to replay them all anyway
        session.DeleteWhere<DeadLetterEvent>(x => x.ProjectionName == source.ProjectionName);

        await session.SaveChangesAsync(token).ConfigureAwait(false);
    }
}
