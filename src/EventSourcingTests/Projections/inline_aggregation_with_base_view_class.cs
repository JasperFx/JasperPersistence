using System;
using System.Linq;
using Marten.Testing.Harness;
using Shouldly;
using Weasel.Core;
using Xunit;

namespace EventSourcingTests.Projections;

public class inline_aggregation_with_base_view_class: OneOffConfigurationsContext
{
    private readonly MonsterSlayed slayed1 = new MonsterSlayed { Name = "Troll" };
    private readonly MonsterSlayed slayed2 = new MonsterSlayed { Name = "Dragon" };
    private readonly Guid streamId;

    public inline_aggregation_with_base_view_class()
    {
        StoreOptions(_ =>
        {
            _.AutoCreateSchemaObjects = AutoCreate.All;
            _.Projections.Snapshot<QuestMonstersWithBaseClass>();
            _.Projections.Snapshot<QuestMonstersWithBaseClassAndIdOverloaded>();
            _.Projections.Snapshot<QuestMonstersWithBaseClassAndIdOverloadedWithNew>();
        });

        streamId = theSession.Events
            .StartStream<QuestMonstersWithBaseClass>(slayed1, slayed2).Id;

        theSession.SaveChanges();
    }

    [Fact]
    public void run_inline_aggregation_with_base_view_class()
    {
        VerifyProjection<QuestMonstersWithBaseClass>();
    }

    [Fact]
    public void run_inline_aggregation_with_base_class_and_id_overloaded()
    {
        VerifyProjection<QuestMonstersWithBaseClassAndIdOverloaded>();
    }

    [Fact]
    public void run_inline_aggregation_with_base_class_and_id_overloaded_with_new()
    {
        VerifyProjection<QuestMonstersWithBaseClassAndIdOverloadedWithNew>();
    }

    private void VerifyProjection<T>() where T : IMonstersView
    {
        var loadedView = theSession.Load<T>(streamId);

        loadedView.ShouldNotBeNull();
        loadedView.Id.ShouldBe(streamId);
        loadedView.Monsters.ShouldHaveTheSameElementsAs("Troll", "Dragon");

        var queriedView = theSession.Query<T>()
            .Single(x => x.Id == streamId);

        queriedView.Id.ShouldBe(streamId);
        queriedView.Monsters.ShouldHaveTheSameElementsAs("Troll", "Dragon");
    }
}