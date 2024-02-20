using System;
using System.Collections.Generic;
using System.Linq;

namespace Marten.Events.Daemon;

/// <summary>
///     Helps control the "pull-based" event loading in an individual projection shard
/// </summary>
internal class ProjectionController
{
    private readonly IShardAgent _agent;

    private readonly Queue<EventRange> _inFlight = new();
    private readonly AsyncOptions _options;
    private readonly ShardName _shardName;


    public ProjectionController(ShardName shardName, IShardAgent agent, AsyncOptions options)
    {
        _shardName = shardName;
        _agent = agent;
        _options = options ?? new AsyncOptions();
    }

    public int InFlightCount => _inFlight.Sum(x => x.Size);

    public long LastEnqueued { get; private set; }

    public long LastCommitted { get; private set; }

    public long HighWaterMark { get; private set; }

    public void MarkHighWater(long sequence)
    {
        // Ignore the high water mark if it's lower than
        // already encountered. Not sure how that could happen,
        // but still be ready for that.
        if (sequence <= HighWaterMark)
        {
            return;
        }

        HighWaterMark = sequence;

        enqueueNewEventRanges();
    }

    public void Start(long highWaterMark, long lastCommitted)
    {
        if (lastCommitted > highWaterMark)
        {
            throw new InvalidOperationException(
                $"The last committed number ({lastCommitted}) cannot be higher than the high water mark ({highWaterMark})");
        }

        HighWaterMark = highWaterMark;
        LastCommitted = LastEnqueued = lastCommitted;


        if (HighWaterMark > 0)
        {
            enqueueNewEventRanges();
        }
    }

    private void enqueueNewEventRanges()
    {
        while (HighWaterMark > LastEnqueued && InFlightCount < _options.MaximumHopperSize)
        {
            var floor = LastEnqueued;
            var ceiling = LastEnqueued + _options.BatchSize;
            if (ceiling > HighWaterMark)
            {
                ceiling = HighWaterMark;
            }

            startRange(floor, ceiling);
        }
    }

    private void startRange(long floor, long ceiling)
    {
        var range = new EventRange(_shardName, floor, ceiling);
        LastEnqueued = range.SequenceCeiling;
        _inFlight.Enqueue(range);
        //_agent.StartRange(range);
    }

    public void EventRangeUpdated(EventRange range)
    {
        LastCommitted = range.SequenceCeiling;
        if (Equals(range, _inFlight.Peek()))
        {
            _inFlight.Dequeue();
        }

        enqueueNewEventRanges();
    }
}
