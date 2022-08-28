using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Marten.Events.Projections;
using Marten.Internal.Sessions;
using Marten.Services;
using Microsoft.Extensions.Logging;
#nullable enable

namespace Marten.Events.Daemon
{
    /// <summary>
    /// Responsible for running a single async projection shard at runtime. Equivalent to V3 ProjectionTrack
    /// </summary>
    internal class ShardAgent : IShardAgent, IObserver<ShardState>
    {
        private SessionOptions? _sessionOptions;

        private readonly DocumentStore _store;
        private readonly AsyncProjectionShard _projectionShard;
        private CancellationToken _cancellation;

        private TransformBlock<EventRange, EventRangeGroup>? _grouping;
        private ActionBlock<Command>? _commandBlock;
        private TransformBlock<EventRange, EventRange>? _loader;
        private IEventFetcher? _fetcher;
        private ShardStateTracker? _tracker;
        private IDisposable? _subscription;
        private ProjectionDaemon? _daemon;
        private CancellationTokenSource? _cancellationSource;
        private ActionBlock<EventRangeGroup>? _building;

        private readonly IProjectionSource _source;
        private readonly ProjectionController _controller;

        public ShardAgent(DocumentStore store, AsyncProjectionShard projectionShard, ILogger logger, CancellationToken cancellation)
        {
            if (cancellation == CancellationToken.None)
            {
                _cancellationSource = new CancellationTokenSource();
                _cancellation = _cancellationSource.Token;
            }

            Name = projectionShard.Name;

            ProjectionShardIdentity = projectionShard.Name.Identity;

            _store = store;
            _projectionShard = projectionShard;
            Logger = logger;
            _cancellation = cancellation;

            _source = projectionShard.Source;

            _controller =
                new ProjectionController(projectionShard.Name, this, projectionShard.Source.Options);
        }

        public ILogger Logger { get; }

        public string ProjectionShardIdentity { get; private set; }

        public ShardName Name { get; }

        public CancellationToken Cancellation => _cancellation;


        private async Task<EventRange> loadEvents(EventRange range)
        {
            var parameters = new ActionParameters(this, async () =>
            {
                await _fetcher!.Load(range, _cancellation).ConfigureAwait(false);

                if (Logger.IsEnabled(LogLevel.Debug))
                {
                    Logger.LogDebug("Loaded events {Range} for {ProjectionShardIdentity}", range, ProjectionShardIdentity);
                }
            })
            {
                LogAction = (logger, e) =>
                {
                    logger.LogError(e, "Error loading events {Range} for {ProjectionShardIdentity}", range, ProjectionShardIdentity);
                }
            };


            await _daemon!.TryAction(parameters).ConfigureAwait(false);

            return range;
        }

        public AgentStatus Status { get; private set; }


        public void StartRange(EventRange range)
        {
            if (_cancellation.IsCancellationRequested) return;

            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug("Enqueued processing {Range} for {ProjectionShardIdentity}", range, ProjectionShardIdentity);
            }

            _loader!.Post(range);
        }

        public Task TryAction(Func<Task> action, CancellationToken token, Action<ILogger, Exception>? logException = null, EventRangeGroup? group = null, GroupActionMode actionMode = GroupActionMode.Parent)
        {
            var parameters = new ActionParameters(this, action, token == default ? _cancellation : token)
            {
                GroupActionMode = actionMode
            };

            parameters.LogAction = logException ?? parameters.LogAction;
            parameters.Group = group;

            return _daemon!.TryAction(parameters);
        }

        public bool IsStopping { get; private set; } = false;

        public async Task<long> Start(ProjectionDaemon daemon, ShardExecutionMode mode)
        {
            Mode = mode;

            if (daemon.Database.Identifier != "Marten")
            {
                ProjectionShardIdentity = $"{ProjectionShardIdentity}@{daemon.Database.Identifier}";
            }

            Logger.LogInformation("Starting projection agent for '{ProjectionShardIdentity}'", ProjectionShardIdentity);

            _sessionOptions = SessionOptions.ForDatabase(daemon.Database);

            initializeDataflowBlocks(daemon);

            var lastCommitted = await daemon.Database.ProjectionProgressFor(_projectionShard.Name, _cancellation).ConfigureAwait(false);

            await ensureStorageExists(daemon).ConfigureAwait(false);

            _subscription = _tracker.Subscribe(this);
            _commandBlock?.Post(Command.Started(_tracker.HighWaterMark, lastCommitted));

            Logger.LogInformation("Projection agent for '{ProjectionShardIdentity}' has started from sequence {LastCommitted} and a high water mark of {HighWaterMark}", ProjectionShardIdentity, lastCommitted, _tracker.HighWaterMark);

            Status = AgentStatus.Running;

            Position = lastCommitted;
            return lastCommitted;
        }

        private async Task ensureStorageExists(ProjectionDaemon daemon)
        {
            foreach (var storageType in _source.Options.StorageTypes)
            {
                await daemon.Database.EnsureStorageExistsAsync(storageType, _cancellation).ConfigureAwait(false);
            }

            foreach (var publishedType in _source.PublishedTypes())
            {
                await daemon.Database.EnsureStorageExistsAsync(publishedType, _cancellation).ConfigureAwait(false);
            }
        }

        public ShardExecutionMode Mode { get; set; } = ShardExecutionMode.Continuous;

        [MemberNotNull(nameof(_commandBlock), nameof(_loader), nameof(_tracker), nameof(_daemon), nameof(_fetcher), nameof(_grouping), nameof(_building))]
        private void initializeDataflowBlocks(ProjectionDaemon daemon)
        {
            var singleFileOptions = new ExecutionDataflowBlockOptions
            {
                EnsureOrdered = true,
                MaxDegreeOfParallelism = 1,
                CancellationToken = _cancellation,
            };

            _commandBlock = new ActionBlock<Command>(command => command.Apply(_controller), singleFileOptions);
            _loader = new TransformBlock<EventRange, EventRange>(loadEvents, singleFileOptions);

            _tracker = daemon.Tracker;
            _daemon = daemon;

            _fetcher = Mode == ShardExecutionMode.Continuous
                ? new EventFetcher(_store, this, _daemon.Database, _projectionShard.EventFilters)
                : new RebuildingEventFetcher(_store, this, _daemon.Database, _projectionShard.EventFilters);

            _grouping = new TransformBlock<EventRange, EventRangeGroup>(groupEventRange, singleFileOptions);

            _building = new ActionBlock<EventRangeGroup>(processRange, singleFileOptions);

            _grouping.LinkTo(_building);

            // The filter is important. You may need to allow an empty page to go through
            // just to keep tracking correct
            _loader.LinkTo(_grouping, e => e.Events != null);
        }

        private async Task processRange(EventRangeGroup group)
        {
            if (_cancellation.IsCancellationRequested) return;

            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug("Shard '{ProjectionShardIdentity}': Starting to process events for {Group}", ProjectionShardIdentity, group);
            }

            // This should be done *once* here before going to the TryAction()
            group.Reset();

            ProjectionUpdateBatch? batch = null;

            // Building the ProjectionUpdateBatch
            await TryAction(async () =>
            {
                batch = await buildUpdateBatch(@group).ConfigureAwait(false);

                @group.Dispose();
            }, _cancellation, (logger, e) =>
            {
                logger.LogError(e, "Failure while trying to process updates for event range {EventRange} for projection shard '{ProjectionShardIdentity}'", @group, ProjectionShardIdentity);
            }, @group:@group).ConfigureAwait(false);

            // This has failed, so get out of here.
            if (batch == null) return;

            // Executing the SQL commands for the ProjectionUpdateBatch
            await TryAction(async () =>
            {
                await ExecuteBatch(batch).ConfigureAwait(false);

                if (Logger.IsEnabled(LogLevel.Debug))
                {
                    Logger.LogDebug("Shard '{ProjectionShardIdentity}': Successfully processed batch {Group}", ProjectionShardIdentity, @group);
                }
            }, _cancellation, (logger, e) =>
            {
                logger.LogError(e, "Failure while trying to process updates for event range {EventRange} for projection shard '{ProjectionShardIdentity}'", @group, ProjectionShardIdentity);
            }).ConfigureAwait(false);
        }

        private async Task<ProjectionUpdateBatch?> buildUpdateBatch(EventRangeGroup @group)
        {
            if (group.Cancellation.IsCancellationRequested) return null; // get out of here early instead of letting it linger

            var batch = StartNewBatch(group);

            try
            {
                await @group.ConfigureUpdateBatch(this, batch).ConfigureAwait(false);

                if (group.Cancellation.IsCancellationRequested)
                {
                    if (group.Exception != null)
                    {
                        ExceptionDispatchInfo.Capture(group.Exception).Throw();
                    }

                    return batch; // get out of here early instead of letting it linger
                }

                batch.Queue.Complete();
                await batch.Queue.Completion.ConfigureAwait(false);

                if (group.Exception != null)
                {
                    ExceptionDispatchInfo.Capture(group.Exception).Throw();
                }
            }
            finally
            {
                if (batch != null)
                {
                    await batch.CloseSession().ConfigureAwait(false);
                }
            }

            return batch;
        }

        private async Task<EventRangeGroup> groupEventRange(EventRange range)
        {
            if (_cancellation.IsCancellationRequested) return null;

            EventRangeGroup? group = null;

            await TryAction(async () =>
            {
                if (Logger.IsEnabled(LogLevel.Debug))
                {
                    Logger.LogDebug("Shard '{ProjectionShardIdentity}':Starting to group {Range}", ProjectionShardIdentity, range);
                }

                @group = await _source.GroupEvents(_store, _daemon!.Database, range, _cancellation).ConfigureAwait(false);

                if (Logger.IsEnabled(LogLevel.Debug))
                {
                    Logger.LogDebug("Shard '{ProjectionShardIdentity}': successfully grouped {Range}", ProjectionShardIdentity, range);
                }

            }, _cancellation, (logger, e) =>
            {
                logger.LogError(e, "Error while trying to group event range {EventRange} for projection shard {SProjectionShardIdentity}", range, ProjectionShardIdentity);
            }).ConfigureAwait(false);



            return group;
        }

        public Task Stop(Exception? ex = null)
        {
            IsStopping = true;

            Logger.LogInformation("Stopping projection shard '{ProjectionShardIdentity}'", ProjectionShardIdentity);

            _cancellationSource?.Cancel();

            _commandBlock?.Complete();
            _loader?.Complete();
            _grouping?.Complete();
            _building?.Complete();

            _subscription?.Dispose();

            _fetcher?.Dispose();

            _subscription = null;
            _fetcher = null;
            _commandBlock = null;
            _grouping = null;
            _loader = null;
            _building = null;

            Logger.LogInformation("Stopped projection shard '{ProjectionShardIdentity}'", ProjectionShardIdentity);

            _tracker?.Publish(new ShardState(_projectionShard.Name, Position)
            {
                Action = ShardAction.Stopped,
                Exception = ex
            });

            IsStopping = false;

            return Task.CompletedTask;
        }

        public async Task Pause(TimeSpan timeout)
        {
            await Stop().ConfigureAwait(false);

            Status = AgentStatus.Paused;
            _tracker!.Publish(new ShardState(_projectionShard, ShardAction.Paused));

#pragma warning disable 4014
            // ReSharper disable once MethodSupportsCancellation
            Task.Run(async () =>
#pragma warning restore 4014
            {
                // ReSharper disable once MethodSupportsCancellation
                await Task.Delay(timeout).ConfigureAwait(false);
                _cancellationSource = new CancellationTokenSource();
                _cancellation = _cancellationSource.Token;

                var parameters = new ActionParameters(this, async () =>
                {
                    try
                    {
                        await Start(_daemon!, ShardExecutionMode.Continuous).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        throw new ShardStartException(this, e);
                    }
                });

                parameters.LogAction = (l, e) =>
                {
                    l.LogError(e, "Error trying to start shard '{ProjectionShardIdentity}' after pausing",
                        ProjectionShardIdentity);
                };

                await _daemon!.TryAction(parameters).ConfigureAwait(false);
            });
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
                    Logger.LogDebug("Projection Shard '{ProjectionShardIdentity}' received high water mark at {Sequence}", ProjectionShardIdentity, value.Sequence);
                }

                _commandBlock!.Post(
                    Command.HighWaterMarkUpdated(value.Sequence));
            }
        }

        public ShardName ShardName => _projectionShard.Name;

        public ProjectionUpdateBatch StartNewBatch(EventRangeGroup group)
        {
            var session = _store.OpenSession(_sessionOptions!);
            return new ProjectionUpdateBatch(_store.Events, _store.Options.Projections, (DocumentSessionBase) session, @group.Range, @group.Cancellation, Mode);
        }

        public async Task ExecuteBatch(ProjectionUpdateBatch batch)
        {
            if (_cancellation.IsCancellationRequested || batch == null) return;

            await batch.Queue.Completion.ConfigureAwait(false);

            if (_cancellation.IsCancellationRequested) return;

            var session = (DocumentSessionBase)_store.OpenSession(_sessionOptions!);
            await using (session.ConfigureAwait(false))
            {
                try
                {
                    await session.ExecuteBatchAsync(batch, _cancellation).ConfigureAwait(false);

                    Logger.LogInformation("Shard '{ProjectionShardIdentity}': Executed updates for {Range}", ProjectionShardIdentity, batch.Range);
                }
                catch (Exception e)
                {
                    if (!_cancellation.IsCancellationRequested)
                    {
                        Logger.LogError(e,
                            "Failure in shard '{ProjectionShardIdentity}' trying to execute an update batch for {Range}", ProjectionShardIdentity,
                            batch.Range);
                        throw;
                    }
                }
                finally
                {
                    batch.Dispose();
                }
            }

            batch.Dispose();

            if (_cancellation.IsCancellationRequested) return;

            Position = batch.Range.SequenceCeiling;

            _tracker?.Publish(new ShardState(ShardName, batch.Range.SequenceCeiling){Action = ShardAction.Updated});

            _commandBlock?.Post(Command.Completed(batch.Range));
        }

        public long Position { get; private set; }

        public async Task<long> DrainSerializationFailureRecording()
        {
            if (_fetcher is RebuildingEventFetcher rebuild)
            {
                return await rebuild.Complete().ConfigureAwait(false);
            }

            return 0;
        }
    }
}
