﻿using System;
using System.Linq;
using Xunit;
using Marten.Testing.Documents;
using Npgsql;
using Shouldly;

namespace Marten.Testing.Schema
{
    public class create_database_Tests : IDisposable
    {
        [Fact]
        public void can_create_new_database_when_one_does_not_exist_for_default_tenant()
        {
            var cstring = ConnectionSource.ConnectionString;

            TryDropDb(dbName);

            using (var store1 = DocumentStore.For(_ =>
            {
                _.Connection(dbToCreateConnectionString);
            }))
            {
                Assert.Throws<PostgresException>(() =>
                {
                    store1.Schema.ApplyAllConfiguredChangesToDatabase();
                });
            }

            using (var store = DocumentStore.For(_ =>
            {
                _.Connection(dbToCreateConnectionString);
                _.PLV8Enabled = false;
                _.CreateDatabasesForTenants(c =>
                {
                    c.MaintenanceDatabase(cstring);
                    c.ForTenant()
                        .CheckAgainstPgDatabase()
                        .WithOwner("postgres")
                        .WithEncoding("UTF-8")
                        .ConnectionLimit(-1);
                });
            }))
            {                
                store.Schema.ApplyAllConfiguredChangesToDatabase();
                store.Schema.AssertDatabaseMatchesConfiguration();
            }
        }

        [Fact]
        public void can_use_existing_database_without_calling_into_create()
        {
            var user1 = new User { FirstName = "User" };
            var dbCreated = false;
            using (var store = DocumentStore.For(_ =>
            {
                _.Connection(ConnectionSource.ConnectionString);
                _.CreateDatabasesForTenants(c =>
                {
                    c.MaintenanceDatabase(ConnectionSource.ConnectionString);
                    c.ForTenant()
                        .CheckAgainstPgDatabase()
                        .WithOwner("postgres")
                        .WithEncoding("UTF-8")
                        .ConnectionLimit(-1)
                        .OnDatabaseCreated(___ => dbCreated = true);
                });

            }))
            {
                using (var session = store.OpenSession())
                {
                    session.Store(user1);
                    session.SaveChanges();
                }

                Assert.False(dbCreated);
            }
        }

        private readonly string dbToCreateConnectionString;
        private readonly string dbName;

        private static Tuple<string, string> DbToCreate(string cstring)
        {
            var builder = new NpgsqlConnectionStringBuilder(cstring);
            builder.Database = $"_dropme{DateTime.UtcNow.Ticks}_{builder.Database}";
            return Tuple.Create(builder.ToString(), builder.Database);
        }

        public create_database_Tests()
        {
            var db = DbToCreate(ConnectionSource.ConnectionString);
            dbToCreateConnectionString = db.Item1;
            dbName = db.Item2;
        }

        private static bool TryDropDb(string db)
        {
            try
            {
                using (var connection = new NpgsqlConnection(ConnectionSource.ConnectionString))
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        connection.Open();
                        // Ensure connections to DB are killed - there seems to be a lingering idle session after AssertDatabaseMatchesConfiguration(), even after store disposal
                        cmd.CommandText +=
                            $"SELECT pg_terminate_backend(pg_stat_activity.pid) FROM pg_stat_activity WHERE pg_stat_activity.datname = '{db}' AND pid <> pg_backend_pid();";
                        cmd.CommandText += $"DROP DATABASE IF EXISTS {db};";
                        cmd.ExecuteNonQuery();
                    }                    
                    finally
                    {
                        connection.Close();
                        connection.Dispose();
                    }
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public void Dispose()
        {
            TryDropDb(dbName);
        }
    }
}