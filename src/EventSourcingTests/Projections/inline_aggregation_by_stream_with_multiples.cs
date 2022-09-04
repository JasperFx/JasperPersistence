using System.Threading.Tasks;
using Marten;
using Marten.Storage;
using Marten.Testing.Harness;
using Weasel.Core;
using Xunit;

namespace EventSourcingTests.Projections;

public class inline_aggregation_by_stream_with_multiples: OneOffConfigurationsContext
{
    private readonly QuestStarted started = new QuestStarted { Name = "Find the Orb" };
    private readonly MembersJoined joined = new MembersJoined { Day = 2, Location = "Faldor's Farm", Members = new string[] { "Garion", "Polgara", "Belgarath" } };
    private readonly MonsterSlayed slayed1 = new MonsterSlayed { Name = "Troll" };
    private readonly MonsterSlayed slayed2 = new MonsterSlayed { Name = "Dragon" };

    private readonly MembersJoined joined2 = new MembersJoined { Day = 5, Location = "Sendaria", Members = new string[] { "Silk", "Barak" } };

    [Theory]
    [InlineData(TenancyStyle.Single)]
    [InlineData(TenancyStyle.Conjoined)]
    public void run_multiple_aggregates_sync(TenancyStyle tenancyStyle)
    {
        #region sample_registering-quest-party
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
        #endregion

        StoreOptions(_ =>
        {
            _.AutoCreateSchemaObjects = AutoCreate.All;
            _.Projections.SelfAggregate<QuestParty>();
            _.Projections.SelfAggregate<QuestMonsters>();
        });

        var streamId = theSession.Events
            .StartStream<QuestParty>(started, joined, slayed1, slayed2, joined2).Id;
        theSession.SaveChanges();

        theSession.Load<QuestMonsters>(streamId).Monsters.ShouldHaveTheSameElementsAs("Troll", "Dragon");

        theSession.Load<QuestParty>(streamId).Members
            .ShouldHaveTheSameElementsAs("Garion", "Polgara", "Belgarath", "Silk", "Barak");
    }

    [Fact]
    public async Task run_multiple_aggregates_async()
    {
        StoreOptions(_ =>
        {
            _.AutoCreateSchemaObjects = AutoCreate.All;

            _.Projections.SelfAggregate<QuestMonsters>();
            _.Projections.SelfAggregate<QuestParty>();
        });

        var streamId = theSession.Events
            .StartStream<QuestParty>(started, joined, slayed1, slayed2, joined2).Id;
        await theSession.SaveChangesAsync();

        (await theSession.LoadAsync<QuestMonsters>(streamId)).Monsters.ShouldHaveTheSameElementsAs("Troll", "Dragon");

        (await theSession.LoadAsync<QuestParty>(streamId)).Members
            .ShouldHaveTheSameElementsAs("Garion", "Polgara", "Belgarath", "Silk", "Barak");
    }

}