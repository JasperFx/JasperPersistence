using System.Linq;
using System.Threading.Tasks;
using EventSourcingTests.Projections;
using Marten;
using Marten.Services.Json;
using Marten.Testing.Harness;
using Npgsql;
using Shouldly;
using Xunit;

namespace EventSourcingTests;

public class flexible_event_metadata: OneOffConfigurationsContext
{
    private readonly QuestStarted started = new QuestStarted { Name = "Find the Orb" };

    private readonly MembersJoined joined = new MembersJoined
    {
        Day = 2, Location = "Faldor's Farm", Members = new string[] { "Garion", "Polgara", "Belgarath" }
    };

    private readonly MonsterSlayed slayed = new MonsterSlayed { Name = "Troll" };

    [Fact]
    public async Task check_metadata_correlation_id_enabled()
    {
        StoreOptions(_ => _.Events.MetadataConfig.CorrelationIdEnabled = true);
        const string correlationId = "test-correlation-id";
        TheSession.CorrelationId = correlationId;

        var streamId = TheSession.Events
            .StartStream<QuestParty>(started, joined, slayed).Id;
        await TheSession.SaveChangesAsync();

        var events = await TheSession.Events.FetchStreamAsync(streamId);
        foreach (var @event in events)
        {
            @event.CorrelationId.ShouldBe(correlationId);
        }
    }

    [Fact]
    public async Task check_search_with_correlation_id()
    {
        StoreOptions(_ => _.Events.MetadataConfig.CorrelationIdEnabled = true);
        const string correlationId = "test-correlation-id";
        TheSession.CorrelationId = correlationId;

        var streamId = TheSession.Events.StartStream<QuestParty>(started, joined, slayed).Id;
        await TheSession.SaveChangesAsync();

        var events = await TheSession.Events.QueryAllRawEvents()
            .Where(x => x.CorrelationId == correlationId)
            .ToListAsync();

        events.Count.ShouldBe(3);
        events[0].StreamId.ShouldBe(streamId);
        events[1].StreamId.ShouldBe(streamId);
        events[2].StreamId.ShouldBe(streamId);
    }

    [Fact]
    public async Task check_metadata_correlation_id_disabled()
    {
        // note: by default CorrelationId meta data is not enabled
        const string correlationId = "test-correlation-id";
        TheSession.CorrelationId = correlationId;

        var streamId = TheSession.Events
            .StartStream<QuestParty>(started, joined, slayed).Id;
        await TheSession.SaveChangesAsync();

        var events = await TheSession.Events.FetchStreamAsync(streamId);
        foreach (var @event in events)
        {
            @event.CorrelationId.ShouldBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task check_metadata_causation_id_enabled()
    {
        StoreOptions(_ => _.Events.MetadataConfig.CausationIdEnabled = true);
        const string causationId = "test-causation-id";
        TheSession.CausationId = causationId;

        var streamId = TheSession.Events
            .StartStream<QuestParty>(started, joined, slayed).Id;
        await TheSession.SaveChangesAsync();

        var events = await TheSession.Events.FetchStreamAsync(streamId);
        foreach (var @event in events)
        {
            @event.CausationId.ShouldBe(causationId);
        }
    }

    [Fact]
    public async Task check_search_with_causation_id()
    {
        StoreOptions(_ => _.Events.MetadataConfig.CausationIdEnabled = true);
        const string causationId = "test-causation-id";
        TheSession.CausationId = causationId;

        var streamId = TheSession.Events.StartStream<QuestParty>(started, joined, slayed).Id;
        await TheSession.SaveChangesAsync();

        var events = await TheSession.Events.QueryAllRawEvents()
            .Where(x => x.CausationId == causationId)
            .ToListAsync();

        events.Count.ShouldBe(3);
        events[0].StreamId.ShouldBe(streamId);
        events[1].StreamId.ShouldBe(streamId);
        events[2].StreamId.ShouldBe(streamId);
    }

    [Fact]
    public async Task check_metadata_causation_id_disabled()
    {
        // note: by default CausationId meta data is not enabled
        const string causationId = "test-causation-id";
        TheSession.CausationId = causationId;

        var streamId = TheSession.Events
            .StartStream<QuestParty>(started, joined, slayed).Id;
        await TheSession.SaveChangesAsync();

        var events = await TheSession.Events.FetchStreamAsync(streamId);
        foreach (var @event in events)
        {
            @event.CausationId.ShouldBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task check_user_defined_metadata_enabled()
    {
        StoreOptions(_ => _.Events.MetadataConfig.HeadersEnabled = true);
        const string userDefinedMetadataName = "my-custom-metadata";
        const string userDefinedMetadataValue = "my-custom-metadata-value";
        TheSession.SetHeader(userDefinedMetadataName, userDefinedMetadataValue);

        var streamId = TheSession.Events
            .StartStream<QuestParty>(started, joined, slayed).Id;
        await TheSession.SaveChangesAsync();

        var events = await TheSession.Events.FetchStreamAsync(streamId);
        foreach (var @event in events)
        {
            var retrievedVal = @event.GetHeader(userDefinedMetadataName).ToString();
            retrievedVal.ShouldBe(userDefinedMetadataValue);
        }
    }

    [Fact]
    public async Task check_user_defined_metadata_disabled()
    {
        // note: by default user defined meta data is not enabled
        const string userDefinedMetadataName = "my-custom-metadata";
        const string userDefinedMetadataValue = "my-custom-metadata-value";
        TheSession.SetHeader(userDefinedMetadataName, userDefinedMetadataValue);

        var streamId = TheSession.Events
            .StartStream<QuestParty>(started, joined, slayed).Id;
        await TheSession.SaveChangesAsync();

        var events = await TheSession.Events.FetchStreamAsync(streamId);
        foreach (var @event in events)
        {
            @event.GetHeader(userDefinedMetadataName).ShouldBeNull();
        }
    }

    [Fact]
    public async Task check_user_defined_metadata_not_exists()
    {
        // Fixes Issue #2100
        StoreOptions(_ => _.Events.MetadataConfig.HeadersEnabled = true);
        const string userDefinedMetadata1Name = "my-custom-metadata-1";
        const string userDefinedMetadata1Value = "my-custom-metadata-1-value";
        TheSession.SetHeader(userDefinedMetadata1Name, userDefinedMetadata1Value);

        var streamId = TheSession.Events
            .StartStream<QuestParty>(started, joined, slayed).Id;
        await TheSession.SaveChangesAsync();


        const string userDefinedMetadata2Name = "my-custom-metadata-2";
        var events = await TheSession.Events.FetchStreamAsync(streamId);
        foreach (var @event in events)
        {
            @event.GetHeader(userDefinedMetadata2Name).ShouldBeNull();
        }
    }

    [Fact]
    public async Task check_flexible_metadata_with_all_enabled()
    {
        StoreOptions(_ => _.Events.MetadataConfig.EnableAll());

        const string correlationId = "test-correlation-id";
        TheSession.CorrelationId = correlationId;

        const string causationId = "test-causation-id";
        TheSession.CausationId = causationId;

        const string userDefinedMetadataName = "my-custom-metadata";
        const string userDefinedMetadataValue = "my-custom-metadata-value";
        TheSession.SetHeader(userDefinedMetadataName, userDefinedMetadataValue);

        var streamId = TheSession.Events
            .StartStream<QuestParty>(started, joined, slayed).Id;
        await TheSession.SaveChangesAsync();

        var events = await TheSession.Events.FetchStreamAsync(streamId);
        foreach (var @event in events)
        {
            @event.CorrelationId.ShouldBe(correlationId);
            @event.CausationId.ShouldBe(causationId);

            var retrievedVal = @event.GetHeader(userDefinedMetadataName).ToString();
            retrievedVal.ShouldBe(userDefinedMetadataValue);
        }
    }

    [Fact]
    public async Task check_writing_empty_headers_system_text_json()
    {
        StoreOptions(_ =>
        {
            _.Events.MetadataConfig.EnableAll();
            _.UseDefaultSerialization(serializerType: SerializerType.SystemTextJson);
        });

        var streamId = TheSession.Events
            .StartStream<QuestParty>(started).Id;
        await TheSession.SaveChangesAsync();
        // Should not throw System.NullReferenceException here
    }

    [Fact]
    public async Task check_writing_empty_headers_newtonsoft_json()
    {
        StoreOptions(_ =>
        {
            _.Events.MetadataConfig.EnableAll();
            _.UseDefaultSerialization(serializerType: SerializerType.Newtonsoft);
        });

        var streamId = TheSession.Events
            .StartStream<QuestParty>(started).Id;
        await TheSession.SaveChangesAsync();
        // Should not throw System.NullReferenceException here
    }
}
