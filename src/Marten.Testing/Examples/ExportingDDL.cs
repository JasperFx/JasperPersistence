using System.Diagnostics;
using System.Threading.Tasks;
using Marten.Testing.Documents;

namespace Marten.Testing.Examples
{
    public class ExportingDDL
    {
        #region sample_export-ddl
        public async Task export_ddl()
        {
            var store = DocumentStore.For(_ =>
            {
                _.Connection("some connection string");

                // If you are depending upon attributes for customization,
                // you have to help DocumentStore "know" what the document types
                // are
                _.Schema.For<User>();
                _.Schema.For<Company>();
                _.Schema.For<Issue>();
            });

            // Export the SQL to a file
            await store.Schema.WriteCreationScriptToFile("my_database.sql");

            // Or instead, write a separate sql script
            // to the named directory
            // for each type of document
            store.Schema.WriteDatabaseCreationScriptByType("sql");

            // or just see it
            var sql = store.Schema.ToDatabaseScript();
            Debug.WriteLine(sql);
        }

        #endregion
    }
}
