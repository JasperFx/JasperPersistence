using System;
using System.Collections.Generic;
using System.Linq;
using Baseline;
using Marten.Schema;
using Marten.Services;

namespace Marten.Events
{
    public class EventStreamAppender : IDocumentUpsert
    {
        private readonly EventGraph _graph;

        public EventStreamAppender(EventGraph graph)
        {
            _graph = graph;

            AppendEventFunction = new FunctionName(_graph.DatabaseSchemaName, "mt_append_event");
        }

        public FunctionName AppendEventFunction { get; }

        public void RegisterUpdate(UpdateBatch batch, object entity)
        {
            var stream = entity.As<EventStream>();

            var streamTypeName = stream.AggregateType == null ? null : _graph.AggregateAliasFor(stream.AggregateType);

            var events = stream.Events.ToArray();
            var eventTypes = events.Select(x => _graph.EventMappingFor(x.Data.GetType()).EventTypeName).ToArray();
            var ids = events.Select(x => x.Id).ToArray();

            var sprocCall = batch.Sproc(AppendEventFunction, new EventStreamVersioningCallback(stream))
                .Param("stream", stream.Id)
                .Param("stream_type", streamTypeName)
                .Param("event_ids", ids)
                .Param("event_types", eventTypes);

            AddJsonBodies(batch, sprocCall, events);
        }

        static void AddJsonBodies(UpdateBatch batch, SprocCall sprocCall, IEvent[] events)
        {
            var serializer = batch.Serializer;

            if (batch.UseCharBufferPooling)
            {
                var segments = new ArraySegment<char>[events.Length];
                for (var i = 0; i < events.Length; i++)
                {
                    segments[i] = SerializeToCharArraySegment(batch, serializer, events[i].Data);
                }
                sprocCall.JsonBodies("bodies", segments);
            }
            else
            {
                sprocCall.JsonBodies("bodies", events.Select(x => serializer.ToJson(x.Data)).ToArray());
            }
        }

        static ArraySegment<char> SerializeToCharArraySegment(UpdateBatch batch, ISerializer serializer, object data)
        {
            var writer = batch.GetWriter();
            serializer.ToJson(data, writer);
            return new ArraySegment<char>(writer.Buffer, 0, writer.Size);
        }

        public void RegisterUpdate(UpdateBatch batch, object entity, string json)
        {
            throw new NotSupportedException();
        }
    }
}