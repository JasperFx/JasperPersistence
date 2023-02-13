using System.Linq;
using Marten.Testing.Documents;
using Marten.Testing.Harness;
using Xunit;

namespace Marten.Testing.Examples;

public class MultiTenancy
{
    [Fact]
    public void use_multiple_tenants()
    {
        // Set up a basic DocumentStore with multi-tenancy
        // via a tenant_id column
        var store = DocumentStore.For(storeOptions =>
        {
            // Bookkeeping;)
            storeOptions.DatabaseSchemaName = "multi1";

            // This sets up the DocumentStore to be multi-tenanted
            // by a tenantid column
            storeOptions.Connection(ConnectionSource.ConnectionString);

            #region sample_tenancy-configure-through-policy

            storeOptions.Policies.AllDocumentsAreMultiTenanted();
            // Shorthand for
            // storeOptions.Policies.ForAllDocuments(_ => _.TenancyStyle = TenancyStyle.Conjoined);

            #endregion
        });

        store.Advanced.Clean.CompletelyRemoveAll();

        #region sample_tenancy-scoping-session-write

        // Write some User documents to tenant "tenant1"
        using (var session = store.LightweightSession("tenant1"))
        {
            session.Store(new User { UserName = "Bill" });
            session.Store(new User { UserName = "Lindsey" });
            session.SaveChanges();
        }

        #endregion

        // Write some User documents to tenant "tenant2"
        using (var session = store.LightweightSession("tenant2"))
        {
            session.Store(new User { UserName = "Jill" });
            session.Store(new User { UserName = "Frank" });
            session.SaveChanges();
        }

        #region sample_tenancy-scoping-session-read

        // When you query for data from the "tenant1" tenant,
        // you only get data for that tenant
        using (var query = store.QuerySession("tenant1"))
        {
            query.Query<User>()
                .Select(x => x.UserName)
                .ToList()
                .ShouldHaveTheSameElementsAs("Bill", "Lindsey");
        }

        #endregion

        using (var query = store.QuerySession("tenant2"))
        {
            query.Query<User>()
                .Select(x => x.UserName)
                .ToList()
                .ShouldHaveTheSameElementsAs("Jill", "Frank");
        }
    }
}
