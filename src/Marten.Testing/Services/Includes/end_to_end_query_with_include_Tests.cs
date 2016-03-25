﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Marten.Services;
using Marten.Services.Includes;
using Marten.Util;
using Octokit;
using Shouldly;
using Xunit;
using Issue = Marten.Testing.Documents.Issue;
using User = Marten.Testing.Documents.User;

namespace Marten.Testing.Services.Includes
{
    public class end_to_end_query_with_include_Tests : DocumentSessionFixture<IdentityMap>
    {
        [Fact]
        public void simple_include_for_a_single_document()
        {
            var user = new User();
            var issue = new Issue {AssigneeId = user.Id, Title = "Garage Door is busted"};

            theSession.Store<object>(user, issue);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                User included = null;
                var issue2 = query.Query<Issue>()
                    .Include<User>(x => x.AssigneeId, x => included = x)
                    .Where(x => x.Title == issue.Title)
                    .Single();

                included.ShouldNotBeNull();
                included.Id.ShouldBe(user.Id);

                issue2.ShouldNotBeNull();
            }
        }

        [Fact]
        public void include_with_containment_where_for_a_single_document()
        {
            var user = new User();
            var issue = new Issue {AssigneeId = user.Id, Tags = new []{"DIY"}, Title = "Garage Door is busted"};

            theSession.Store<object>(user, issue);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                User included = null;
                var issue2 = query.Query<Issue>()
                    .Include<User>(x => x.AssigneeId, x => included = x)
                    .Where(x => x.Tags.Contains("DIY"))
                    .Single();

                included.ShouldNotBeNull();
                included.Id.ShouldBe(user.Id);

                issue2.ShouldNotBeNull();
            }
        }

        [Fact]
        public void include_with_any_containment_where_for_a_single_document()
        {
            var user = new User();
            var issue = new Issue {AssigneeId = user.Id, Tags = new []{"DIY"}, Title = "Garage Door is busted"};

            theSession.Store<object>(user, issue);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                User included = null;
                var issue2 = query.Query<Issue>()
                    .Include<User>(x => x.AssigneeId, x => included = x)
                    .Where(x => x.Tags.Any(t=>t=="DIY"))
                    .Single();

                included.ShouldNotBeNull();
                included.Id.ShouldBe(user.Id);

                issue2.ShouldNotBeNull();
            }
        }

        [Fact]
        public void simple_include_for_a_single_document_using_outer_join()
        {
            var issue = new Issue { AssigneeId = null, Title = "Garage Door is busted" };

            theSession.Store(issue);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                User included = null;
                var issue2 = query.Query<Issue>()
                    .Include<User>(x => x.AssigneeId, x => included = x, JoinType.LeftOuter)
                    .Where(x => x.Title == issue.Title)
                    .Single();

                included.ShouldBeNull();

                issue2.ShouldNotBeNull();
            }
        }

        [Fact]
        public void include_to_list()
        {
            var user1 = new User();
            var user2 = new User();

            var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
            var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
            var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };

            theSession.Store(user1, user2);
            theSession.Store(issue1, issue2, issue3);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                var list = new List<User>();

                query.Query<Issue>().Include<User>(x => x.AssigneeId, list).ToArray();

                list.Count.ShouldBe(2);

                list.Any(x => x.Id == user1.Id);
                list.Any(x => x.Id == user2.Id);
            }
        }

        [Fact]
        public void include_is_running_through_identitymap()
        {
            var user1 = new User();
            var user2 = new User();

            var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
            var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
            var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };

            theSession.Store(user1, user2);
            theSession.Store(issue1, issue2, issue3);
            theSession.SaveChanges();

            // This will only work with a non-NulloIdentityMap
            using (var query = theStore.OpenSession())
            {
                var dict = new Dictionary<Guid, User>();

                query.Query<Issue>().Include(x => x.AssigneeId, dict).ToArray();

                query.Load<User>(user1.Id).ShouldBeSameAs(dict[user1.Id]);
                query.Load<User>(user2.Id).ShouldBeSameAs(dict[user2.Id]);
            }
        }

        [Fact]
        public void include_to_dictionary()
        {
            var user1 = new User();
            var user2 = new User();

            var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
            var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
            var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };

            theSession.Store(user1, user2);
            theSession.Store(issue1, issue2, issue3);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {
                var dict = new Dictionary<Guid, User>();

                query.Query<Issue>().Include(x => x.AssigneeId, dict).ToArray();

                dict.Count.ShouldBe(2);
                dict.ContainsKey(user1.Id).ShouldBeTrue();
                dict.ContainsKey(user2.Id).ShouldBeTrue();
            }


        }


        [Fact]
        public async Task simple_include_for_a_single_document_async()
        {
            var user = new User();
            var issue = new Issue { AssigneeId = user.Id, Title = "Garage Door is busted" };

            theSession.Store<object>(user, issue);
            await theSession.SaveChangesAsync();

            using (var query = theStore.QuerySession())
            {
                User included = null;
                var issue2 = await query.Query<Issue>()
                    .Include<User>(x => x.AssigneeId, x => included = x)
                    .Where(x => x.Title == issue.Title)
                    .SingleAsync();

                included.ShouldNotBeNull();
                included.Id.ShouldBe(user.Id);

                issue2.ShouldNotBeNull();
            }
        }

        [Fact]
        public async Task include_to_list_async()
        {
            var user1 = new User();
            var user2 = new User();

            var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
            var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
            var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };

            theSession.Store(user1, user2);
            theSession.Store(issue1, issue2, issue3);
            await theSession.SaveChangesAsync();

            using (var query = theStore.QuerySession())
            {
                var list = new List<User>();

                await query.Query<Issue>().Include(x => x.AssigneeId, list).ToListAsync();

                list.Count.ShouldBe(2);

                list.Any(x => x.Id == user1.Id);
                list.Any(x => x.Id == user2.Id);
            }



        }

        [Fact]
        public async Task include_to_dictionary_async()
        {
            var user1 = new User();
            var user2 = new User();

            var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
            var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
            var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };

            theSession.Store(user1, user2);
            theSession.Store(issue1, issue2, issue3);
            await theSession.SaveChangesAsync();

            using (var query = theStore.QuerySession())
            {
                var dict = new Dictionary<Guid, User>();

                await query.Query<Issue>().Include(x => x.AssigneeId, dict).ToListAsync();

                dict.Count.ShouldBe(2);
                dict.ContainsKey(user1.Id).ShouldBeTrue();
                dict.ContainsKey(user2.Id).ShouldBeTrue();
            }


        }


        [Fact]
        public void multiple_includes()
        {
            var assignee = new User();
            var reporter = new User();

            var issue1 = new Issue { AssigneeId = assignee.Id, ReporterId = reporter.Id, Title = "Garage Door is busted" };

            theSession.Store(assignee, reporter);
            theSession.Store(issue1);
            theSession.SaveChanges();

            using (var query = theStore.QuerySession())
            {

                User assignee2 = null;
                User reporter2 = null;

                query
                    .Query<Issue>()
                    .Include<User>(x => x.AssigneeId, x => assignee2 = x)
                    .Include<User>(x => x.ReporterId, x => reporter2 = x).Single()
                    .ShouldNotBeNull();

                assignee2.Id.ShouldBe(assignee.Id);
                reporter2.Id.ShouldBe(reporter.Id);

            }
        }


    }
}