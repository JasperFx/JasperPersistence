using System;
using Marten.Testing.Harness;
using Xunit;

namespace Marten.Testing.Bugs
{
    public class Bug_957_duplicating_a_string_array: IntegrationContext
    {
        [Fact]
        public void works_just_fine_on_the_first_cut()
        {
            StoreOptions(_ =>
            {
            });

            theStore.Tenancy.Default.EnsureStorageExists(typeof(HistoryDoc));

            var store2 = DocumentStore.For(_ =>
            {
                _.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
                _.Connection(ConnectionSource.ConnectionString);
                _.Schema.For<HistoryDoc>().Duplicate(x => x.UrlHistory, pgType: "text[]");
            });

            store2.Tenancy.Default.EnsureStorageExists(typeof(HistoryDoc));
        }

        public Bug_957_duplicating_a_string_array(DefaultStoreFixture fixture) : base(fixture)
        {
        }
    }

    public class HistoryDoc
    {
        public Guid Id { get; set; }
        public string[] UrlHistory { get; set; }
    }
}
