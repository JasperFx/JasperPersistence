﻿using Baseline;
using Marten.Schema;
using Marten.Storage;
using Marten.Testing.Documents;
using Marten.Testing.Harness;
using Shouldly;
using Xunit;

namespace Marten.Testing.Storage
{
    public class will_build_a_new_database_table_if_definition_changes_Tests
    {
        [Fact]
        public void will_build_the_new_table_if_the_configured_table_does_not_match_the_existing_table()
        {
            DocumentTable table1;
            DocumentTable table2;

            using (var store = TestingDocumentStore.Basic())
            {
                store.Tenancy.Default.StorageFor(typeof(User));

                store.Tenancy.Default.DbObjects.DocumentTables().ShouldContain("public.mt_doc_user");

                table1 = store.TableSchema(typeof(User));
                table1.ShouldNotContain(x => x.Name == "user_name");
            }

            using (var store = DocumentStore.For(ConnectionSource.ConnectionString))
            {
                store.Storage.MappingFor(typeof(User)).As<DocumentMapping>().DuplicateField("UserName");

                store.Tenancy.Default.StorageFor(typeof(User));

                store.Tenancy.Default.DbObjects.DocumentTables().ShouldContain("public.mt_doc_user");

                table2 = store.TableSchema(typeof(User));
            }

            table2.ShouldNotBe(table1);

            SpecificationExtensions.ShouldNotBeNull(table2.Column("user_name"));
        }

        [Fact]
        public void will_build_the_new_table_if_the_configured_table_does_not_match_the_existing_table_on_other_schema()
        {
            DocumentTable table1;
            DocumentTable table2;

            using (var store = TestingDocumentStore.For(_ => _.DatabaseSchemaName = "other"))
            {
                store.Tenancy.Default.EnsureStorageExists(typeof(User));

                store.Tenancy.Default.DbObjects.DocumentTables().ShouldContain("other.mt_doc_user");

                table1 = store.TableSchema(typeof(User));
                table1.ShouldNotContain(x => x.Name == "user_name");
            }

            using (var store = DocumentStore.For(_ =>
            {
                _.Connection(ConnectionSource.ConnectionString);
                _.DatabaseSchemaName = "other";
            }))
            {
                store.Storage.MappingFor(typeof(User)).As<DocumentMapping>().DuplicateField("UserName");

                store.Tenancy.Default.EnsureStorageExists(typeof(User));

                store.Tenancy.Default.DbObjects.DocumentTables().ShouldContain("other.mt_doc_user");

                table2 = store.TableSchema(typeof(User));
            }


            table2.ShouldNotBe(table1);

            SpecificationExtensions.ShouldNotBeNull(table2.Column("user_name"));
        }
    }
}
