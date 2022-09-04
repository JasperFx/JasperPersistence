﻿using System.Linq;
using System.Threading.Tasks;
using Marten;
using Marten.Testing.Documents;
using Marten.Testing.Harness;
using Xunit;

namespace DocumentDbTests.Reading.Linq;

public class query_running_through_the_IdentityMap_Tests : IntegrationContext
{
    private User user1;
    private User user2;
    private User user3;
    private User user4;

    public query_running_through_the_IdentityMap_Tests(DefaultStoreFixture fixture) : base(fixture)
    {

    }

    protected override async Task fixtureSetup()
    {
        DocumentTracking = DocumentTracking.IdentityOnly;

        #region sample_using-store-with-multiple-docs
        user1 = new User {FirstName = "Jeremy"};
        user2 = new User {FirstName = "Jens"};
        user3 = new User {FirstName = "Jeff"};
        user4 = new User {FirstName = "Corey"};

        theSession.Store(user1, user2, user3, user4);
        #endregion

        await theSession.SaveChangesAsync();

        (await theSession.LoadAsync<User>(user1.Id)).ShouldBeTheSameAs(user1);
    }

    [Fact]
    public void single_runs_through_the_identity_map()
    {
        theSession.Query<User>()
            .Single(x => x.FirstName == "Jeremy").ShouldBeTheSameAs(user1);

        theSession.Query<User>()
            .SingleOrDefault(x => x.FirstName == user4.FirstName).ShouldBeTheSameAs(user4);


    }


    [Fact]
    public void first_runs_through_the_identity_map()
    {
        theSession.Query<User>().Where(x => x.FirstName.StartsWith("J")).OrderBy(x => x.FirstName)
            .First().ShouldBeTheSameAs(user3);


        theSession.Query<User>().Where(x => x.FirstName.StartsWith("J")).OrderBy(x => x.FirstName)
            .FirstOrDefault().ShouldBeTheSameAs(user3);

    }

    [Fact]
    public void query_runs_through_identity_map()
    {
        var users = theSession.Query<User>().Where(x => x.FirstName.StartsWith("J")).OrderBy(x => x.FirstName)
            .ToArray();

        users[0].ShouldBeTheSameAs(user3);
        users[1].ShouldBeTheSameAs(user2);
        users[2].ShouldBeTheSameAs(user1);

    }

    [Fact]
    public async Task single_runs_through_the_identity_map_async()
    {
        var u1 = await theSession.Query<User>().Where(x => x.FirstName == "Jeremy")
            .SingleAsync();

        u1.ShouldBeTheSameAs(user1);

        var u2 = await theSession.Query<User>().Where(x => x.FirstName == user4.FirstName)
            .SingleOrDefaultAsync();

        u2.ShouldBeTheSameAs(user4);


    }



    [Fact]
    public async Task first_runs_through_the_identity_map_async()
    {
        var u1 = await theSession.Query<User>().Where(x => x.FirstName.StartsWith("J")).OrderBy(x => x.FirstName)
            .FirstAsync();

        u1.ShouldBeTheSameAs(user3);


        var u2 = await theSession.Query<User>().Where(x => x.FirstName.StartsWith("J")).OrderBy(x => x.FirstName)
            .FirstOrDefaultAsync();

        u2.ShouldBeTheSameAs(user3);

    }


    [Fact]
    public async Task query_runs_through_identity_map_async()
    {
        var users = await theSession.Query<User>().Where(x => x.FirstName.StartsWith("J")).OrderBy(x => x.FirstName)
            .ToListAsync();

        users[0].ShouldBeTheSameAs(user3);
        users[1].ShouldBeTheSameAs(user2);
        users[2].ShouldBeTheSameAs(user1);

    }
}