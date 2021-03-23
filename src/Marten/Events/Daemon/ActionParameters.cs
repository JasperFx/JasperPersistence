using System;
using System.Threading;
using System.Threading.Tasks;
using Marten.Events.Daemon.Resiliency;
using Microsoft.Extensions.Logging;

namespace Marten.Events.Daemon
{
    internal class ActionParameters
    {
        public ShardAgent Shard { get; }
        public Func<Task> Action { get; }
        public CancellationToken Cancellation { get; private set; }

        public ActionParameters(Func<Task> action, CancellationToken cancellation) : this(null, action, cancellation)
        {

        }

        public ActionParameters(ShardAgent shard, Func<Task> action) : this(shard, action, shard.Cancellation)
        {

        }

        public ActionParameters(ShardAgent shard, Func<Task> action, CancellationToken cancellation)
        {
            Cancellation = cancellation;

            Shard = shard;
            Action = action;

            LogAction = (logger, ex) =>
            {
                logger.LogError(ex, "Error in Async Projection '{ShardName}' / '{Message}'", Shard.ShardName.Identity,
                    ex.Message);
            };
        }

        public int Attempts { get; private set; } = 0;
        public TimeSpan Delay { get; private set; } = default;

        public Action<ILogger, Exception> LogAction { get; set; }
        public EventRangeGroup Group { get; set; }

        public void IncrementAttempts(TimeSpan delay = default)
        {
            Attempts++;
            Delay = delay;
        }

        public void ApplySkip(SkipEvent skip)
        {

            Group?.SkipEventSequence(skip.Event.Sequence);

            // You have to reset the CancellationToken for the group
            Group?.Reset();

            Cancellation = Group?.Cancellation ?? Cancellation;


            // Basically saying that the attempts start over when we skip
            Attempts = 0;
            Delay = default;
        }
    }
}
