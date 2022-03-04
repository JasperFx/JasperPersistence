using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Marten;
using Marten.Schema;
using Marten.Testing;
using Marten.Testing.Documents;
using Marten.Testing.Harness;
using Shouldly;
using Weasel.Postgresql.Tables;
using Xunit;

namespace DocumentDbTests.Indexes
{
    public class computed_indexes: OneOffConfigurationsContext
    {
        [Fact]
        public void example()
        {
            #region sample_using-a-simple-calculated-index
            var store = DocumentStore.For(_ =>
            {
                _.Connection(ConnectionSource.ConnectionString);

                _.DatabaseSchemaName = "examples";

                // This creates
                _.Schema.For<User>().Index(x => x.UserName);
            });

            using (var session = store.QuerySession())
            {
                // Postgresql will be able to use the computed
                // index generated from above
                var somebody = session
                    .Query<User>()
                    .FirstOrDefault(x => x.UserName == "somebody");
            }
            #endregion

            store.Dispose();
        }

        [Fact]
        public async Task smoke_test()
        {
            StoreOptions(_ => _.Schema.For<Target>().Index(x => x.Number));

            var data = Target.GenerateRandomData(100).ToArray();
            await theStore.BulkInsertAsync(data.ToArray());

            var table = await theStore.Tenancy.Default.Database.ExistingTableFor(typeof(Target));
            table.HasIndex("mt_doc_target_idx_number").ShouldBeTrue();

            using var session = theStore.QuerySession();
            var cmd = session.Query<Target>().Where(x => x.Number == 3)
                .ToCommand();

            session.Query<Target>().Where(x => x.Number == data.First().Number)
                .Select(x => x.Id).ToList().ShouldContain(data.First().Id);
        }

        [Fact]
        public void specify_a_deep_index()
        {
            #region sample_deep-calculated-index
            var store = DocumentStore.For(_ =>
            {
                _.Connection(ConnectionSource.ConnectionString);

                _.Schema.For<Target>().Index(x => x.Inner.Color);
            });
            #endregion
        }

        [Fact]
        public void specify_a_different_mechanism_to_customize_the_index()
        {
            #region sample_customizing-calculated-index
            var store = DocumentStore.For(_ =>
            {
                _.Connection(ConnectionSource.ConnectionString);

                // The second, optional argument to Index()
                // allows you to customize the calculated index
                _.Schema.For<Target>().Index(x => x.Number, x =>
                        {
                            // Change the index method to "brin"
                            x.Method = IndexMethod.brin;

                            // Force the index to be generated with casing rules
                            x.Casing = ComputedIndex.Casings.Lower;

                            // Override the index name if you want
                            x.Name = "mt_my_name";

                            // Toggle whether or not the index is concurrent
                            // Default is false
                            x.IsConcurrent = true;

                            // Toggle whether or not the index is a UNIQUE
                            // index
                            x.IsUnique = true;

                            // Toggle whether index value will be constrained unique in scope of whole document table (Global)
                            // or in a scope of a single tenant (PerTenant)
                            // Default is Global
                            x.TenancyScope = Marten.Schema.Indexing.Unique.TenancyScope.PerTenant;

                            // Partial index by supplying a condition
                            x.Predicate = "(data ->> 'Number')::int > 10";
                        });

                // For B-tree indexes, it's also possible to change
                // the sort order from the default of "ascending"
                _.Schema.For<User>().Index(x => x.LastName, x =>
                        {
                            // Change the index method to "brin"
                            x.SortOrder = SortOrder.Desc;
                        });
            });
            #endregion
        }



        [Fact]
        public async Task create_multi_property_index()
        {
            StoreOptions(_ =>
            {
                var columns = new Expression<Func<Target, object>>[]
                {
                    x => x.UserId,
                    x => x.Flag
                };
                _.Schema.For<Target>().Index(columns);
            });

            var data = Target.GenerateRandomData(100).ToArray();
            await theStore.BulkInsertAsync(data.ToArray());

            var table = await theStore.Tenancy.Default.Database.ExistingTableFor(typeof(Target));
            var index = table.IndexFor("mt_doc_target_idx_user_idflag");

            index.ToDDL(table).ShouldBe("CREATE INDEX mt_doc_target_idx_user_idflag ON computed_indexes.mt_doc_target USING btree (CAST(data ->> 'UserId' as uuid), CAST(data ->> 'Flag' as boolean));");
        }

        [Fact]
        public async Task create_multi_property_string_index_with_casing()
        {
            StoreOptions(_ =>
            {
                var columns = new Expression<Func<Target, object>>[]
                {
                    x => x.String,
                    x => x.StringField
                };
                _.Schema.For<Target>().Index(columns, c => c.Casing = ComputedIndex.Casings.Upper);
            });

            var data = Target.GenerateRandomData(100).ToArray();
            await theStore.BulkInsertAsync(data.ToArray());

            var table = await theStore.Tenancy.Default.Database.ExistingTableFor(typeof(Target));
            var index = table.IndexFor("mt_doc_target_idx_stringstring_field");

            index.ToDDL(table).ShouldBe("CREATE INDEX mt_doc_target_idx_stringstring_field ON computed_indexes.mt_doc_target USING btree (upper((data ->> 'String'), upper((data ->> 'StringField'))));");


        }

        [Fact]
        public void creating_index_using_date_should_work()
        {
            StoreOptions(_ =>
            {
                _.Schema.For<Target>().Index(x => x.Date);
            });

            var data = Target.GenerateRandomData(100).ToArray();
            theStore.BulkInsert(data.ToArray());

        }


        [Fact]
        public async Task create_index_with_custom_name()
        {
            StoreOptions(_ => _.Schema.For<Target>().Index(x => x.String, x =>
            {
                x.Name = "mt_banana_index_created_by_nigel";
            }));

            var testString = "MiXeD cAsE sTrInG";

            using (var session = theStore.LightweightSession())
            {
                var item = Target.GenerateRandomData(1).First();
                item.String = testString;
                session.Store(item);
                await session.SaveChangesAsync();
            }

            (await theStore.Tenancy.Default.Database.ExistingTableFor(typeof(Target)))
                .HasIndex("mt_banana_index_created_by_nigel");

        }


        [Fact]
        public async Task patch_if_missing()
        {
            using (var store1 = SeparateStore())
            {
                await store1.Advanced.Clean.CompletelyRemoveAllAsync();

                await store1.EnsureStorageExistsAsync(typeof(Target));
            }

            using (var store2 = DocumentStore.For(_ =>
            {
                _.Connection(ConnectionSource.ConnectionString);
                _.Schema.For<Target>().Index(x => x.Number);
            }))
            {
                var patch = await store2.Schema.CreateMigrationAsync();

                patch.UpdateSql().ShouldContain( "mt_doc_target_idx_number", Case.Insensitive);
            }
        }

        [Fact]
        public async Task create_index_on_collection()
        {
            StoreOptions(_ => _.Schema.For<Target>().Index(x => x.StringList));

            using (var session = theStore.LightweightSession())
            {
                var item = Target.GenerateRandomData(1).First();
                item.StringList.Add("item1");
                item.StringList.Add("item2");
                session.Store(item);
                await session.SaveChangesAsync();
            }

            var table = await theStore.Tenancy.Default.Database.ExistingTableFor(typeof(Target));
            var index = table.IndexFor("mt_doc_target_idx_string_list");

            index.ToDDL(table).ShouldBe("CREATE INDEX mt_doc_target_idx_string_list ON computed_indexes.mt_doc_target USING btree (CAST(data ->> 'StringList' as jsonb));");

        }

        [Fact]
        public async Task create_multi_index_including_collection()
        {
            var columns = new Expression<Func<Target, object>>[]
            {
                x => x.UserId,
                x => x.StringList
            };

            StoreOptions(_ => _.Schema.For<Target>().Index(columns));

            using (var session = theStore.LightweightSession())
            {
                var item = Target.GenerateRandomData(1).First();
                item.UserId = Guid.NewGuid();
                item.StringList.Add("item1");
                item.StringList.Add("item2");
                session.Store(item);
                await session.SaveChangesAsync();
            }

            var table = await theStore.Tenancy.Default.Database.ExistingTableFor(typeof(Target));
            var index = table.IndexFor("mt_doc_target_idx_user_idstring_list");

            index.ToDDL(table).ShouldBe("CREATE INDEX mt_doc_target_idx_user_idstring_list ON computed_indexes.mt_doc_target USING btree (CAST(data ->> 'UserId' as uuid), CAST(data ->> 'StringList' as jsonb));");

        }

    }
}
