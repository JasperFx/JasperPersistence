using System;
using System.Threading.Tasks;
using Marten.Events;
using Marten.Events.Aggregation;
using Marten.Events.Projections;
using Marten.Testing.Harness;
using Shouldly;
using Xunit;

namespace EventSourcingTests.Aggregation;

#region sample_apply_metadata

public class using_apply_metadata : OneOffConfigurationsContext
{
    [Fact]
    public async Task apply_metadata()
    {
        StoreOptions(opts =>
        {
            opts.Projections.Add<ItemProjection>(ProjectionLifecycle.Inline);

            // THIS IS NECESSARY FOR THIS SAMPLE!
            opts.Events.MetadataConfig.HeadersEnabled = true;
        });

        // Setting a header value on the session, which will get tagged on each
        // event captured by the current session
        theSession.SetHeader("last-modified-by", "Glenn Frey");

        var id = theSession.Events.StartStream<Item>(new ItemStarted("Blue item")).Id;
        await theSession.SaveChangesAsync();

        theSession.Events.Append(id, new ItemWorked(), new ItemWorked(), new ItemFinished());
        await theSession.SaveChangesAsync();

        var item = await theSession.LoadAsync<Item>(id);

        // RIP Glenn Frey, take it easy!
        item.LastModifiedBy.ShouldBe("Glenn Frey");
    }
}

#endregion

#region sample_using_ApplyMetadata

public class Item
{
    public Guid Id { get; set; }
    public string Description { get; set; }
    public bool Started { get; set; }
    public DateTimeOffset WorkedOn { get; set; }
    public bool Completed { get; set; }
    public string LastModifiedBy { get; set; }
    public DateTimeOffset? LastModified { get; set; }
}

public record ItemStarted(string Description);

public record ItemWorked;

public record ItemFinished;

public class ItemProjection: SingleStreamProjection<Item>
{
    public void Apply(Item item, ItemStarted started)
    {
        item.Started = true;
        item.Description = started.Description;
    }

    public void Apply(Item item, IEvent<ItemWorked> worked)
    {
        // Nothing, I know, this is weird
    }

    public void Apply(Item item, ItemFinished finished)
    {
        item.Completed = true;
    }

    public override Item ApplyMetadata(Item aggregate, IEvent lastEvent)
    {
        // Apply the last timestamp
        aggregate.LastModified = lastEvent.Timestamp;

        if (lastEvent.Headers.TryGetValue("last-modified-by", out var person))
        {
            aggregate.LastModifiedBy = person?.ToString() ?? "System";
        }

        return aggregate;
    }
}

#endregion
