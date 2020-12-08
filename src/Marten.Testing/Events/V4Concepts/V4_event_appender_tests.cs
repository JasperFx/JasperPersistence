using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Baseline;
using ImTools;
using Marten.Events;
using Marten.Events.V4Concept.CodeGeneration;
using Marten.Exceptions;
using Marten.Internal.Sessions;
using Marten.Storage;
using Marten.Testing.Harness;
using Xunit;
using Shouldly;
using Xunit.Abstractions;

namespace Marten.Testing.Events.V4Concepts
{

    [Collection("v4events")]
    public class V4_event_appender_tests
    {
        private readonly ITestOutputHelper _output;

        public V4_event_appender_tests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void generate_operation_builder(TestCase @case)
        {
            EventDocumentStorageGenerator.GenerateStorage(@case.Store.Options)
                .ShouldNotBeNull();
        }

        [Theory]
        [MemberData(nameof(Data))]
        public async Task can_fetch_stream_async(TestCase @case)
        {
            @case.Store.Advanced.Clean.CompletelyRemoveAll();
            @case.StartNewStream(new TestOutputMartenLogger(_output));
            using var query = @case.Store.QuerySession();

            var builder = EventDocumentStorageGenerator.GenerateStorage(@case.Store.Options);
            var handler = builder.QueryForStream(@case.ToEventStream());

            var state = await query.As<QuerySession>().ExecuteHandlerAsync(handler, CancellationToken.None);
            state.ShouldNotBeNull();
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void can_fetch_stream_sync(TestCase @case)
        {
            @case.Store.Advanced.Clean.CompletelyRemoveAll();
            @case.StartNewStream();
            using var query = @case.Store.QuerySession();

            var builder = EventDocumentStorageGenerator.GenerateStorage(@case.Store.Options);
            var handler = builder.QueryForStream(@case.ToEventStream());

            var state = query.As<QuerySession>().ExecuteHandler(handler);
            state.ShouldNotBeNull();
        }

        [Theory]
        [MemberData(nameof(Data))]
        public async Task can_insert_a_new_stream(TestCase @case)
        {
            // This is just forcing the store to start the event storage
            @case.Store.Advanced.Clean.CompletelyRemoveAll();
            @case.StartNewStream();

            var stream = @case.CreateNewStream();
            var builder = EventDocumentStorageGenerator.GenerateStorage(@case.Store.Options);
            var op = builder.InsertStream(stream);

            using var session = @case.Store.LightweightSession();
            session.QueueOperation(op);

            await session.SaveChangesAsync();
        }

        [Theory]
        [MemberData(nameof(Data))]
        public async Task can_update_the_version_of_an_existing_stream_happy_path(TestCase @case)
        {
            @case.Store.Advanced.Clean.CompletelyRemoveAll();
            var stream = @case.StartNewStream(new TestOutputMartenLogger(_output));

            stream.ExpectedVersionOnServer = 4;
            stream.Version = 10;

            var builder = EventDocumentStorageGenerator.GenerateStorage(@case.Store.Options);
            var op = builder.UpdateStreamVersion(stream);

            using var session = @case.Store.LightweightSession();
            session.QueueOperation(op);

            session.Logger = new TestOutputMartenLogger(_output);
            await session.SaveChangesAsync();

            var handler = builder.QueryForStream(stream);
            var state = session.As<QuerySession>().ExecuteHandler(handler);

            state.Version.ShouldBe(10);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public async Task can_update_the_version_of_an_existing_stream_sad_path(TestCase @case)
        {
            @case.Store.Advanced.Clean.CompletelyRemoveAll();
            var stream = @case.StartNewStream();

            stream.ExpectedVersionOnServer = 3; // it's actually 4, so this should fail
            stream.Version = 10;

            var builder = EventDocumentStorageGenerator.GenerateStorage(@case.Store.Options);
            var op = builder.UpdateStreamVersion(stream);

            using var session = @case.Store.LightweightSession();
            session.QueueOperation(op);

            await Should.ThrowAsync<EventStreamUnexpectedMaxEventIdException>(() => session.SaveChangesAsync());
        }

        public static IEnumerable<object[]> Data()
        {
            return cases().Select(x => new object[] {x});
        }

        private static IEnumerable<TestCase> cases()
        {
            yield return new TestCase("Streams as Guid, Vanilla", e => e.StreamIdentity = StreamIdentity.AsGuid);
            yield return new TestCase("Streams as String, Vanilla", e => e.StreamIdentity = StreamIdentity.AsString);

            yield return new TestCase("Streams as Guid, Multi-tenanted", e =>
            {
                e.StreamIdentity = StreamIdentity.AsGuid;
                e.TenancyStyle = TenancyStyle.Conjoined;
            });

            yield return new TestCase("Streams as String, Multi-tenanted", e =>
            {
                e.StreamIdentity = StreamIdentity.AsString;
                e.TenancyStyle = TenancyStyle.Conjoined;
            });
        }

        public class TestCase : IDisposable
        {
            private readonly string _description;

            public TestCase(string description, Action<EventGraph> config)
            {
                _description = description;

                Store = DocumentStore.For(opts =>
                {
                    config(opts.Events);
                    opts.Connection(ConnectionSource.ConnectionString);
                    opts.DatabaseSchemaName = "v4events";
                    opts.AutoCreateSchemaObjects = AutoCreate.All;
                });

                Store.Advanced.Clean.CompletelyRemoveAll();

                StreamId = Guid.NewGuid();
                TenantId = "KC";
            }

            public StreamAction StartNewStream(IMartenSessionLogger logger = null)
            {
                var events = new object[] {new AEvent(), new BEvent(), new CEvent(), new DEvent()};
                using var session = Store.Events.TenancyStyle == TenancyStyle.Conjoined
                    ? Store.LightweightSession(TenantId)
                    : Store.LightweightSession();

                if (logger != null)
                {
                    session.Logger = logger;
                }

                if (Store.Events.StreamIdentity == StreamIdentity.AsGuid)
                {
                    session.Events.StartStream(StreamId, events);
                    session.SaveChanges();

                    var stream = StreamAction.Start(StreamId);
                    stream.Version = 4;
                    stream.TenantId = TenantId;

                    return stream;
                }
                else
                {
                    session.Events.StartStream(StreamId.ToString(), events);
                    session.SaveChanges();

                    var stream = StreamAction.Start(StreamId.ToString());
                    stream.Version = 4;
                    stream.TenantId = TenantId;

                    return stream;
                }
            }

            public StreamAction CreateNewStream()
            {
                var events = new IEvent[] {new Event<AEvent>(new AEvent())};
                var stream = Store.Events.StreamIdentity == StreamIdentity.AsGuid ? StreamAction.Start(Guid.NewGuid(), events) : StreamAction.Start(Guid.NewGuid().ToString(), events, true);

                stream.TenantId = TenantId;
                stream.Version = 1;

                return stream;
            }

            public string TenantId { get; set; }

            public Guid StreamId { get;  }

            public DocumentStore Store { get;  }

            public void Dispose()
            {
                Store?.Dispose();
            }

            public override string ToString()
            {
                return _description;
            }

            public StreamAction ToEventStream()
            {
                if (Store.Events.StreamIdentity == StreamIdentity.AsGuid)
                {
                    var stream = StreamAction.Start(StreamId);
                    stream.TenantId = TenantId;

                    return stream;
                }
                else
                {
                    var stream = StreamAction.Start(StreamId.ToString());
                    stream.TenantId = TenantId;

                    return stream;
                }
            }
        }
    }
}
