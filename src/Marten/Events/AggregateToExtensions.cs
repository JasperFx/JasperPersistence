﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Baseline;
using Marten.Linq;

namespace Marten.Events
{
    public static class AggregateToExtensions
    {
        /// <summary>
        /// Aggregate the events in this query to the type T
        /// </summary>
        /// <param name="queryable"></param>
        /// <param name="state"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T AggregateTo<T>(this IMartenQueryable<IEvent> queryable, T state = null) where T : class
        {
            var events = queryable.ToList();
            if (!events.Any())
            {
                return null;
            }

            var session = queryable.As<MartenLinqQueryable<IEvent>>().Session;
            var aggregator = session.Options.Projections.AggregatorFor<T>();

            var aggregate = aggregator.Build(queryable.ToList(), (IQuerySession)session, state);

            return aggregate;
        }

        /// <summary>
        /// Aggregate the events in this query to the type T
        /// </summary>
        /// <param name="queryable"></param>
        /// <param name="state"></param>
        /// <param name="token"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static async Task<T> AggregateToAsync<T>(this IMartenQueryable<IEvent> queryable, T state = null,
                                                        CancellationToken token = new ()) where T : class
        {
            var events = await queryable.ToListAsync(token).ConfigureAwait(false);
            if (!events.Any())
            {
                return null;
            }

            var session = queryable.As<MartenLinqQueryable<IEvent>>().Session;
            var aggregator = session.Options.Projections.AggregatorFor<T>();

            var aggregate = await aggregator.BuildAsync(events, (IQuerySession)session, state, token).ConfigureAwait(false);

            return aggregate;
        }

    }
}
