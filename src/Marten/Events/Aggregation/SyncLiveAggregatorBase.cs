#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Marten.Events.Projections;

namespace Marten.Events.Aggregation;

/// <summary>
///     Internal base class for purely synchronous live aggregators
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class SyncLiveAggregatorBase<T>: ILiveAggregator<T> where T : class
{
    public abstract T Build(IReadOnlyList<IEvent> events, IQuerySession session, T? snapshot);

    public ValueTask<T> BuildAsync(IReadOnlyList<IEvent> events, IQuerySession session, T? snapshot,
        CancellationToken cancellation)
    {
        return new ValueTask<T>(Build(events, session, snapshot));
    }
}
