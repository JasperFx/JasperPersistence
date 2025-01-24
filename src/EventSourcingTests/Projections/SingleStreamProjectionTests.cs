using System;
using EventSourcingTests.Aggregation;
using Marten;
using Marten.Events;
using Marten.Events.Aggregation;
using Marten.Events.Projections;
using Marten.Schema;
using Marten.Storage;
using Marten.Testing.Documents;
using Marten.Util;
using NSubstitute;
using Shouldly;
using Xunit;

namespace EventSourcingTests.Projections;

public class SingleStreamProjectionTests
{
    [Fact]
    public void set_mapping_to_UseVersionFromMatchingStream_when_quick_append()
    {
        var projection = new SingleStreamProjection<User>();
        var mapping = DocumentMapping.For<User>();

        mapping.StoreOptions.EventGraph.AppendMode = EventAppendMode.Quick;
        projection.Lifecycle = ProjectionLifecycle.Inline;

        projection.ConfigureAggregateMapping(mapping, mapping.StoreOptions);

        mapping.UseVersionFromMatchingStream.ShouldBeTrue();
    }

    [Fact]
    public void do_not_set_mapping_to_UseVersionFromMatchingStream_when_rich_append()
    {
        var projection = new SingleStreamProjection<User>();
        var mapping = DocumentMapping.For<User>();

        mapping.StoreOptions.EventGraph.AppendMode = EventAppendMode.Rich;

        projection.ConfigureAggregateMapping(mapping, mapping.StoreOptions);

        mapping.UseVersionFromMatchingStream.ShouldBeFalse();
    }

    [Fact]
    public void you_cannot_accidentally_try_to_add_single_stream_projection_through_the_snapshot()
    {
        var options = new StoreOptions();

        Should.Throw<InvalidOperationException>(() =>
        {
            options.Projections.Snapshot<AllGood>(SnapshotLifecycle.Inline);
        });
    }
}
