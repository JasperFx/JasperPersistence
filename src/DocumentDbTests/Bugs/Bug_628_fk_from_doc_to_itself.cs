using System;
using System.Threading.Tasks;
using Marten.Testing.Harness;
using Xunit;

namespace DocumentDbTests.Bugs
{
    public class Bug_628_fk_from_doc_to_itself: BugIntegrationContext
    {
        public class Category
        {
            public Guid Id { get; set; }
            public Guid? ParentId { get; set; }
            public string Name { get; set; }
        }

        [Fact]
        public async Task can_reference_itself_as_an_fk()
        {
            StoreOptions(_ =>
            {
                _.Schema.For<Category>().ForeignKey<Category>(x => x.ParentId);
            });

            await theStore.Schema.ApplyAllConfiguredChangesToDatabaseAsync();
        }

    }
}
