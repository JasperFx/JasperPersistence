using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Marten.Linq;

namespace Marten.Events
{
    public interface IEventStore
    {
        /// <summary>
        /// Append one or more events in order to an existing stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="events"></param>
        void Append(Guid stream, params object[] events);

        /// <summary>
        /// Append one or more events in order to an existing stream and verify that maximum event id for the stream
        /// matches supplied expected version or transaction is aborted.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="expectedVersion">Expected maximum event version after append</param>
        /// <param name="events"></param>
        void Append(Guid stream, int expectedVersion, params object[] events);

        /// <summary>
        /// Creates a new event stream based on a user-supplied Guid and appends the events in order to the new stream
        /// </summary>
        /// <typeparam name="TAggregate"></typeparam>
        /// <param name="id"></param>
        /// <param name="events"></param>
        /// <returns></returns>
        Guid StartStream<TAggregate>(Guid id, params object[] events) where TAggregate : class, new();

        /// <summary>
        /// Creates a new event stream, assigns a new Guid id, and appends the events in order to the new stream
        /// </summary>
        /// <typeparam name="TAggregate"></typeparam>
        /// <param name="events"></param>
        /// <returns></returns>
        Guid StartStream<TAggregate>(params object[] events) where TAggregate : class, new();

        /// <summary>
        /// Synchronously fetches all of the events for the named stream
        /// </summary>
        /// <param name="streamId"></param>
        /// <param name="version">If set, queries for events up to and including this version</param>
        /// <param name="timestamp">If set, queries for events captured on or before this timestamp</param>
        /// <param name="limit">If set, limits the number of events returned</param>
        /// <returns></returns>
        IList<IEvent> FetchStream(Guid streamId, int version = 0, DateTime? timestamp = null, int? limit = null);

        /// <summary>
        /// Synchronously fetches all of the events for the named stream
        /// </summary>
        /// <param name="streamId"></param>
        /// <param name="version">If set, queries for events up to and including this version</param>
        /// <param name="timestamp">If set, queries for events captured on or before this timestamp</param>
        /// <param name="limit">If set, limits the number of events returned</param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<IList<IEvent>> FetchStreamAsync(Guid streamId, int version = 0, DateTime? timestamp = null, int? limit = null, CancellationToken token = default(CancellationToken));

        /// <summary>
        /// Perform a live aggregation of the raw events in this stream to a T object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="streamId"></param>
        /// <param name="version"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        T AggregateStream<T>(Guid streamId, int version = 0, DateTime? timestamp = null) where T : class, new();

        /// <summary>
        /// Perform a live aggregation of the raw events in this stream to a T object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="streamId"></param>
        /// <param name="version"></param>
        /// <param name="timestamp"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<T> AggregateStreamAsync<T>(Guid streamId, int version = 0, DateTime? timestamp = null, CancellationToken token = default(CancellationToken)) where T : class, new();


        /// <summary>
        /// Query directly against ONLY the raw event data. Use IQuerySession.Query() for aggregated documents!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IMartenQueryable<T> QueryRawEventDataOnly<T>();


        /// <summary>
        /// Query directly against the raw event data across all event types
        /// </summary>
        /// <returns></returns>
        IMartenQueryable<IEvent> QueryAllRawEvents();


        /// <summary>
        /// Load a single event by its id knowing the event type upfront
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        Event<T> Load<T>(Guid id) where T : class;

        /// <summary>
        /// Load a single event by its id knowing the event type upfront
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<Event<T>> LoadAsync<T>(Guid id, CancellationToken token = default(CancellationToken)) where T : class;

        /// <summary>
        /// Load a single event by its id 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IEvent Load(Guid id);

        /// <summary>
        /// Load a single event by its id 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<IEvent> LoadAsync(Guid id, CancellationToken token = default(CancellationToken));

        /// <summary>
        /// Fetches only the metadata about a stream by id
        /// </summary>
        /// <param name="streamId"></param>
        /// <returns></returns>
        StreamState FetchStreamState(Guid streamId);

        /// <summary>
        /// Fetches only the metadata about a stream by id
        /// </summary>
        /// <param name="streamId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<StreamState> FetchStreamStateAsync(Guid streamId, CancellationToken token = default(CancellationToken));
    }
}