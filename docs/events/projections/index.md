# Projections

::: warning
The programming model for projections was completely rewritten for Marten V4
:::

Marten has a (we hope) strong model for user-defined projections of the raw event data. Projections are used within Marten to create
read-side views of the raw event data. The basics of the Marten projection model are shown below:

![Projection Class Diagram](/images/Projections.png)

Do note that all the various types of aggregated projections all inherit from a common base and have the same core set of conventions.

## Projection Types

1. [Aggregate Projections](/events/projections/aggregate-projections) combine either a stream or some other related set of events into a single view.
1. [View Projections](/events/projections/view-projections) are a specialized form of aggregate projections that allow you to aggregate against arbitrary groupings of events across streams.
1. [Event Projections](/events/projections/event-projections) are a recipe for building projections that create or delete one or more documents for a single event
1. If one of the built in projection recipes doesn't fit what you want to do, you can happily build your own [custom projection](/events/projections/custom)

## Projection Lifecycles

Marten varies a little bit in that projections can be executed with three different lifecycles:

1. [Inline Projections](/events/projections/inline) are executed at the time of event capture and in the same unit of work to persist the projected documents
1. [Live Aggregations](/events/projections/live-aggregates) are executed on demand by loading event data and creating the projected view in memory without persisting the projected documents
1. [Asynchronous Projections](/events/projections/async-daemon) are executed by a background process

For other descriptions of the _Projections_ pattern inside of Event Sourcing architectures, see:

* [Projections in Event Sourcing](https://zimarev.com/blog/event-sourcing/projections/)
* [Projections in Event Sourcing: Build ANY model you want!](https://codeopinion.com/projections-in-event-sourcing-build-any-model-you-want/)

## Aggregates

Aggregates condense data described by a single stream. As of v1.0, Marten only supports aggregation via .Net classes. Aggregates are calculated upon every request by running the event stream through them, as compared to inline projections, which are computed at event commit time and stored as documents.

The out-of-the box convention is to expose `public Apply([Event Type])` methods on your aggregate class to do all incremental updates to an aggregate object. This can be customized using [AggregatorLookup](#aggregator-lookup).

Sticking with the fantasy theme, the `QuestParty` class shown below could be used to aggregate streams of quest data:

<!-- snippet: sample_QuestParty -->
<a id='snippet-sample_questparty'></a>
```cs
public class QuestParty
{
    public List<string> Members { get; set; } = new();
    public IList<string> Slayed { get; } = new List<string>();
    public string Key { get; set; }
    public string Name { get; set; }

    // In this particular case, this is also the stream id for the quest events
    public Guid Id { get; set; }

    // These methods take in events and update the QuestParty
    public void Apply(MembersJoined joined) => Members.Fill(joined.Members);
    public void Apply(MembersDeparted departed) => Members.RemoveAll(x => departed.Members.Contains(x));
    public void Apply(QuestStarted started) => Name = started.Name;

    public override string ToString()
    {
        return $"Quest party '{Name}' is {Members.Join(", ")}";
    }
}
```
<sup><a href='https://github.com/JasperFx/marten/blob/master/src/EventSourcingTests/Projections/QuestParty.cs#L8-L30' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample_questparty' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

New in Marten 1.2 is the ability to use `Event<T>` metadata within your projections, assuming that you're not trying to run the aggregations inline.

The syntax using the built in aggregation technique is to take in `Event<T>` as the argument to your `Apply(event)` methods,
where `T` is the event type you're interested in:

<!-- snippet: sample_QuestPartyWithEvents -->
<a id='snippet-sample_questpartywithevents'></a>
```cs
public class QuestPartyWithEvents
{
    private readonly IList<string> _members = new List<string>();

    public string[] Members
    {
        get
        {
            return _members.ToArray();
        }
        set
        {
            _members.Clear();
            _members.AddRange(value);
        }
    }

    public IList<string> Slayed { get; } = new List<string>();

    public void Apply(MembersJoined joined)
    {
        _members.Fill(joined.Members);
    }

    public void Apply(MembersDeparted departed)
    {
        _members.RemoveAll(x => departed.Members.Contains(x));
    }

    public void Apply(QuestStarted started)
    {
        Name = started.Name;
    }

    public string Name { get; set; }

    public Guid Id { get; set; }

    public override string ToString()
    {
        return $"Quest party '{Name}' is {Members.Join(", ")}";
    }
}
```
<sup><a href='https://github.com/JasperFx/marten/blob/master/src/EventSourcingTests/Projections/QuestPartyWithEvents.cs#L8-L53' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample_questpartywithevents' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Aggregates Across Multiple Streams

Example coming soon, and check [Jeremy's blog](http://jeremydmiller.com) for a sample soon.

It's possible currently by using either a custom `IProjection` or using the existing aggregation capabilities with a
custom `IAggregateFinder<T>`, where `T` is the projected view document type.

## Aggregator Lookup

`EventGraph.UseAggregatorLookup(IAggregatorLookup aggregatorLookup)` can be used to register an `IAggregatorLookup` that is used to look up `IAggregator<T>` for aggregations. This allows a generic aggregation strategy to be used, rather than registering aggregators case-by-case through `EventGraphAddAggregator<T>(IAggregator<T> aggregator)`.

A shorthand extension method `EventGraph.UseAggregatorLookup(this EventGraph eventGraph, AggregationLookupStrategy strategy)` can be used to set default aggregation lookup, whereby

* `AggregationLookupStrategy.UsePublicApply` resolves aggregators that use public Apply
* `AggregationLookupStrategy.UsePrivateApply` resolves aggregators that use private Apply
* `AggregationLookupStrategy.UsePublicAndPrivateApply` resolves aggregators that use public or private Apply

The aggregation lookup can also be set in the `StoreOptions.Events.UserAggregatorLookup`

// TODO: fix this sample
<[sample:register-custom-aggregator-lookup]>

## Live Aggregation via .Net

You can always fetch a stream of events and build an aggregate completely live from the current event data by using this syntax:

<!-- snippet: sample_events-aggregate-on-the-fly -->
<a id='snippet-sample_events-aggregate-on-the-fly'></a>
```cs
using (var session = store.OpenSession())
{
    // questId is the id of the stream
    var party = session.Events.AggregateStream<QuestParty>(questId);
    Console.WriteLine(party);

    var party_at_version_3 = await session.Events
        .AggregateStreamAsync<QuestParty>(questId, 3);

    var party_yesterday = await session.Events
        .AggregateStreamAsync<QuestParty>(questId, timestamp: DateTime.UtcNow.AddDays(-1));
}
```
<sup><a href='https://github.com/JasperFx/marten/blob/master/src/EventSourcingTests/Examples/event_store_quickstart.cs#L81-L94' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample_events-aggregate-on-the-fly' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

There is also a matching asynchronous `AggregateStreamAsync()` mechanism as well. Additionally, you can do stream aggregations in batch queries with
`IBatchQuery.Events.AggregateStream<T>(streamId)`.

## Inline Projections

_First off, be aware that event metadata (e.g. stream version and sequence number) are not available during the execution of inline projections. If you need to use event metadata in your projections, please use asynchronous or live projections._

If you would prefer that the projected aggregate document be updated _inline_ with the events being appended, you simply need to register the aggregation type in the `StoreOptions` upfront when you build up your document store like this:

<!-- snippet: sample_registering-quest-party -->
<a id='snippet-sample_registering-quest-party'></a>
```cs
var store = DocumentStore.For(_ =>
{
    _.Connection(ConnectionSource.ConnectionString);
    _.Events.TenancyStyle = tenancyStyle;
    _.DatabaseSchemaName = "quest_sample";
    if (tenancyStyle == TenancyStyle.Conjoined)
    {
        _.Schema.For<QuestParty>().MultiTenanted();
    }

    // This is all you need to create the QuestParty projected
    // view
    _.Projections.SelfAggregate<QuestParty>();
});
```
<sup><a href='https://github.com/JasperFx/marten/blob/master/src/EventSourcingTests/Projections/inline_aggregation_by_stream_with_multiples.cs#L24-L39' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample_registering-quest-party' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

At this point, you would be able to query against `QuestParty` as just another document type.

## Rebuilding Projections

Projections need to be rebuilt when the code that defines them changes in a way that requires events to be reapplied in order to maintain correct state. Using an `IDaemon` this is easy to execute on-demand:

Refer to [Rebuilding Projections](/events/projections/rebuilding) for more details.

::: warning
Marten by default while creating new object tries to use <b>default constructor</b>. Default constructor doesn't have to be public, might be also private or protected.

If class does not have the default constructor then it creates an uninitialized object (see [here](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.serialization.formatterservices.getuninitializedobject?view=netframework-4.8) for more info)

Because of that, no member initializers will be run so all of them need to be initialized in the event handler methods.
:::
