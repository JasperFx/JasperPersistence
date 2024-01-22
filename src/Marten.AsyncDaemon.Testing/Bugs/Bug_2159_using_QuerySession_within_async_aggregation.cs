using System;
using System.Threading;
using System.Threading.Tasks;
using EventSourcingTests.Aggregation;
using EventSourcingTests.Projections;
using Marten.Events.Aggregation;
using Marten.Events.Projections;
using Marten.Testing.Documents;
using Marten.Testing.Harness;
using Shouldly;
using Xunit;

namespace Marten.AsyncDaemon.Testing.Bugs;

public class Bug_2159_using_QuerySession_within_async_aggregation : BugIntegrationContext
{
    [Fact]
    public async Task use_query_session_within_async_aggregation()
    {
        StoreOptions(opts => opts.Projections.Add(new UserAggregate(), ProjectionLifecycle.Async));

        var streamId = Guid.NewGuid();
        var user = new User { UserName = "Blue"};

        TheSession.Store(user);
        await TheSession.SaveChangesAsync();

        TheSession.Events.StartStream(streamId, new UserCreated());
        await TheSession.SaveChangesAsync();


        TheSession.Events.Append(streamId, new UserUpdated{UserId = user.Id});
        await TheSession.SaveChangesAsync();

        using var daemon = await TheStore.BuildProjectionDaemonAsync();
        await daemon.RebuildProjection<UserAggregate>(CancellationToken.None);

        var aggregate = await TheSession.LoadAsync<MyAggregate>(streamId);
        aggregate.UpdatedBy.ShouldBe("Blue");

    }
}

public class UserAggregate: SingleStreamProjection<MyAggregate>
{
    public UserAggregate()
    {
        DeleteEvent<UserDeleted>();
    }


    public async Task Apply(UserUpdated @event, MyAggregate aggregate, IQuerySession session)
    {
        var user = await session.LoadAsync<User>(@event.UserId);
        aggregate.UpdatedBy = user.UserName;
    }

    public MyAggregate Create(UserCreated @event)
    {
        return new MyAggregate { };
    }


}