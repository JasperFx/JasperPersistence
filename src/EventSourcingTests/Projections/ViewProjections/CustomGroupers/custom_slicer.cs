﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Marten;
using Marten.Events;
using Marten.Events.Aggregation;
using Marten.Events.Projections;
using Marten.Storage;
using Marten.Testing.Harness;
using Shouldly;
using Xunit;

namespace EventSourcingTests.Projections.ViewProjections.CustomGroupers
{
    #region sample_view-projection-custom-slicer
    public class UserGroupsAssignmentProjection: ViewProjection<UserGroupsAssignment, Guid>
    {
        public class CustomSlicer: IEventSlicer<UserGroupsAssignment, Guid>
        {
            public ValueTask<IReadOnlyList<EventSlice<UserGroupsAssignment, Guid>>> SliceInlineActions(IQuerySession querySession, IEnumerable<StreamAction> streams, ITenancy tenancy)
            {
                var allEvents = streams.SelectMany(x => x.Events).ToList();
                var group = new TenantSliceGroup<UserGroupsAssignment, Guid>(tenancy.Default);
                group.AddEvents<UserRegistered>(@event => @event.UserId, allEvents);
                group.AddEvents<MultipleUsersAssignedToGroup>(@event => @event.UserIds, allEvents);

                return new(group.Slices.ToList());
            }

            public ValueTask<IReadOnlyList<TenantSliceGroup<UserGroupsAssignment, Guid>>> SliceAsyncEvents(IQuerySession querySession, List<IEvent> events, ITenancy tenancy)
            {
                var group = new TenantSliceGroup<UserGroupsAssignment, Guid>(tenancy.Default);
                group.AddEvents<UserRegistered>(@event => @event.UserId, events);
                group.AddEvents<MultipleUsersAssignedToGroup>(@event => @event.UserIds, events);

                return new(new List<TenantSliceGroup<UserGroupsAssignment, Guid>>{group});
            }
        }

        public UserGroupsAssignmentProjection()
        {
            CustomGrouping(new CustomSlicer());
        }

        public void Apply(UserRegistered @event, UserGroupsAssignment view)
        {
            view.Id = @event.UserId;
        }

        public void Apply(MultipleUsersAssignedToGroup @event, UserGroupsAssignment view)
        {
            view.Groups.Add(@event.GroupId);
        }
    }

    #endregion

    public class custom_slicer: OneOffConfigurationsContext
    {
        [Fact]
        public async Task multi_stream_projections_should_work()
        {
            // --------------------------------
            // Create Groups
            // --------------------------------
            // Regular Users
            // Admin Users
            // --------------------------------

            var regularUsersGroupCreated = new UserGroupCreated(Guid.NewGuid(), "Regular Users");
            theSession.Events.Append(regularUsersGroupCreated.GroupId, regularUsersGroupCreated);

            var adminUsersGroupCreated = new UserGroupCreated(Guid.NewGuid(), "Admin Users");
            theSession.Events.Append(adminUsersGroupCreated.GroupId, adminUsersGroupCreated);

            await theSession.SaveChangesAsync();

            // --------------------------------
            // Create Users
            // --------------------------------
            // Anna
            // John
            // Maggie
            // Alan
            // --------------------------------

            var annaRegistered = new UserRegistered(Guid.NewGuid(), "Anna");
            theSession.Events.Append(annaRegistered.UserId, annaRegistered);

            var johnRegistered = new UserRegistered(Guid.NewGuid(), "John");
            theSession.Events.Append(johnRegistered.UserId, johnRegistered);

            var maggieRegistered = new UserRegistered(Guid.NewGuid(), "Maggie");
            theSession.Events.Append(maggieRegistered.UserId, maggieRegistered);

            var alanRegistered = new UserRegistered(Guid.NewGuid(), "Alan");
            theSession.Events.Append(alanRegistered.UserId, alanRegistered);

            await theSession.SaveChangesAsync();

            // --------------------------------
            // Assign users to Groups
            // --------------------------------
            // Anna, Maggie => Admin
            // John, Alan   => Regular
            // --------------------------------

            var annaAndMaggieAssignedToAdminUsersGroup = new MultipleUsersAssignedToGroup(adminUsersGroupCreated.GroupId,
                new List<Guid> {annaRegistered.UserId, maggieRegistered.UserId});
            theSession.Events.Append(annaAndMaggieAssignedToAdminUsersGroup.GroupId,
                annaAndMaggieAssignedToAdminUsersGroup);

            var johnAndAlanAssignedToRegularUsersGroup = new MultipleUsersAssignedToGroup(regularUsersGroupCreated.GroupId,
                new List<Guid> {johnRegistered.UserId, alanRegistered.UserId});
            theSession.Events.Append(johnAndAlanAssignedToRegularUsersGroup.GroupId,
                johnAndAlanAssignedToRegularUsersGroup);

            await theSession.SaveChangesAsync();

            // --------------------------------
            // Check users' groups assignment
            // --------------------------------
            // Anna, Maggie => Admin
            // John, Alan   => Regular
            // --------------------------------

            var annaGroupAssignment = await theSession.LoadAsync<UserGroupsAssignment>(annaRegistered.UserId);
            annaGroupAssignment.ShouldNotBeNull();
            annaGroupAssignment.Id.ShouldBe(annaRegistered.UserId);
            annaGroupAssignment.Groups.ShouldHaveTheSameElementsAs(adminUsersGroupCreated.GroupId);

            var maggieGroupAssignment = await theSession.LoadAsync<UserGroupsAssignment>(maggieRegistered.UserId);
            maggieGroupAssignment.ShouldNotBeNull();
            maggieGroupAssignment.Id.ShouldBe(maggieRegistered.UserId);
            maggieGroupAssignment.Groups.ShouldHaveTheSameElementsAs(adminUsersGroupCreated.GroupId);

            var johnGroupAssignment = await theSession.LoadAsync<UserGroupsAssignment>(johnRegistered.UserId);
            johnGroupAssignment.ShouldNotBeNull();
            johnGroupAssignment.Id.ShouldBe(johnRegistered.UserId);
            johnGroupAssignment.Groups.ShouldHaveTheSameElementsAs(regularUsersGroupCreated.GroupId);

            var alanGroupAssignment = await theSession.LoadAsync<UserGroupsAssignment>(alanRegistered.UserId);
            alanGroupAssignment.ShouldNotBeNull();
            alanGroupAssignment.Id.ShouldBe(alanRegistered.UserId);
            alanGroupAssignment.Groups.ShouldHaveTheSameElementsAs(regularUsersGroupCreated.GroupId);
        }

        public custom_slicer()
        {
            StoreOptions(_ =>
            {
                _.Projections.Add<UserGroupsAssignmentProjection>(ProjectionLifecycle.Inline);
            });
        }
    }
}
