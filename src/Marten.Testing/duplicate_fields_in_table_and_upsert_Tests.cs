﻿using Baseline;
using Marten.Schema;
using Marten.Services;
using Marten.Testing.Documents;
using Shouldly;
using StructureMap;
using Xunit;

namespace Marten.Testing
{
    public class duplicate_fields_in_table_and_upsert_Tests
    {
        [Fact]
        public void end_to_end()
        {
            using (var container = Container.For<DevelopmentModeRegistry>())
            {
                container.GetInstance<DocumentCleaner>().CompletelyRemove(typeof(User));

                var schema = container.GetInstance<IDocumentSchema>();

                container.GetInstance<IDocumentStore>().As<DocumentStore>().Storage.MappingFor(typeof (User)).As<DocumentMapping>().DuplicateField("FirstName");


                var user1 = new User {FirstName = "Byron", LastName = "Scott"};
                using (var session = container.GetInstance<IDocumentStore>().OpenSession())
                {
                    session.Store(user1);
                    session.SaveChanges();
                }

                var runner = container.GetInstance<IManagedConnection>();
                runner.QueryScalar<string>($"select first_name from mt_doc_user where id = '{user1.Id}'")
                    .ShouldBe("Byron");
            }
        } 
    }
}