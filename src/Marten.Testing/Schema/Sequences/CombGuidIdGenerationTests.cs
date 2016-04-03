﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Marten.Schema;
using Marten.Testing.Examples;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Marten.Testing.Schema.Sequences
{
    public class CombGuidIdGenerationTests
    {
        private readonly ITestOutputHelper _output;

        public CombGuidIdGenerationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public class User
        {
            public Guid Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public bool Internal { get; set; }
            public string UserName { get; set; }
        }

        public class UserWithInt
        {
            public Int32 Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public bool Internal { get; set; }
            public string UserName { get; set; }
        }

        public class UserWithString
        {
            public Int32 Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public bool Internal { get; set; }
            public string UserName { get; set; }
        }

        [Fact]
        public void When_ids_are_generated_the_first_id_should_be_less_than_the_second()
        {
            var id1 = Format(CombGuidIdGeneration.NewGuid(new DateTime(2015, 03, 31, 21, 23, 00)));
            var id2 = Format(CombGuidIdGeneration.NewGuid(new DateTime(2015, 03, 31, 21, 23, 01)));

            id1.CompareTo(id2).ShouldBe(-1);
        }

        [Fact]
        public void When_documents_are_stored_after_each_other_then_the_first_id_should_be_less_than_the_second()
        {
            using (
                var container =
                    ContainerFactory.Configure(options => options.DefaultIdStrategy = new CombGuidIdGeneration()))
            {
                container.GetInstance<DocumentCleaner>().CompletelyRemoveAll();
                var store = container.GetInstance<IDocumentStore>();

                StoreUser(store, "User1");
                Thread.Sleep(4); //we need some time inbetween to ensure the timepart of the CombGuid is different
                StoreUser(store, "User2");
                Thread.Sleep(4);
                StoreUser(store, "User3");

                var users = GetUsers(store);

                var id1 = FormatIdAsByteArrayString(users, "User1");
                var id2 = FormatIdAsByteArrayString(users, "User2");
                var id3 = FormatIdAsByteArrayString(users, "User3");

                id1.CompareTo(id2).ShouldBe(-1);
                id2.CompareTo(id3).ShouldBe(-1);
            }
        }

        private static string FormatIdAsByteArrayString(User[] users, string user1)
        {
            var id = users.Single(user => user.LastName == user1).Id;
            return Format(id);
        }

        private static string Format(Guid id)
        {
            var bytes = id.ToByteArray();

            return BitConverter.ToString(bytes);
        }

        private User[] GetUsers(IDocumentStore documentStore)
        {
            using (var session = documentStore.QuerySession())
            {
                return session.Query<User>().ToArray();
            }
        }

        private static void StoreUser(IDocumentStore documentStore, string lastName)
        {
            using (var session = documentStore.OpenSession())
            {
                session.Store(new User { LastName = lastName });
                session.SaveChanges();
            }
        }


        [Fact]
        public void When_ids_are_generated_verify_the_performance()
        {
            using (var container = ContainerFactory.Default())
            {
                container.GetInstance<DocumentCleaner>().CompletelyRemoveAll();
            }

            CheckPerformance<User>("Comb Guid", options =>
            {
                options.DefaultIdStrategy = new CombGuidIdGeneration();
                options.MappingFor(typeof(User)).Alias = "UserWithComb";
            });

            CheckPerformance<User>("Guid", options =>
            {
                options.MappingFor(typeof(User)).Alias = "UserWithGuid";
            });

            CheckPerformance<UserWithInt>("Int", options =>
            {
                options.MappingFor(typeof(UserWithInt)).Alias = "UserWithInt";
            });

            CheckPerformance<UserWithString>("Int", options =>
            {
                options.DefaultIdStrategy = new IdentityKeyGeneration();
                options.MappingFor(typeof(UserWithString)).Alias = "UserWithInt";
            });
        }

        private double CheckPerformance<T>(string title, Action<StoreOptions> options) where T : new()
        {
            using (var container = ContainerFactory.Configure(options))
            {
                var store = container.GetInstance<IDocumentStore>();

                //ramp up
                Insert<T>(store, 10);

                //measure
                var numberOfDocuments = 10000000;
                var start = DateTime.Now;
                Insert<T>(store, numberOfDocuments);
                var totalMilliseconds = (DateTime.Now - start).TotalMilliseconds;

                _output.WriteLine(title + ": " + numberOfDocuments + " " + totalMilliseconds);

                return totalMilliseconds;
            }
        }

        private void Insert<T>(IDocumentStore store, int number) where T : new()
        {
            var batch = 1;
            for (var i = 0; i < number / batch; i++)
            {
                using (var session = store.OpenSession())
                {
                    for (var j = 0; j < batch; j++)
                    {
                        session.Store(new T());
                    }
                    session.SaveChanges();
                }
            }
        }
    }
}