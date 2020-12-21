using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Marten.Events;
using Marten.Events.Projections;
using Marten.Events.V4Concept;
using Marten.Storage;
using Marten.Testing.Harness;
using Shouldly;
using Xunit;

namespace Marten.Testing.Events.Projections.Async
{
    public static class Events
    {
        public class CompanyCreated
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Address { get; set; }
            public string TaxpayerId { get; set; }
        }

        public class CompanyNameChanged
        {
            public Guid Id { get; set; }
            public string NewName { get; set; }
        }

        public class OrderPlaced
        {
            public Guid Id { get; set; }
            public Guid CompanyId { get; set; }
            public decimal TotalAmount { get; set; }
            public string[] Items { get; set; }
        }
    }

    public static class ReadModels
    {
        public class Order
        {
            public Guid Id { get; set; }
            public decimal TotalAmount { get; set; }
            public string CompanyName { get; set; }
            public Guid CompanyId { get; set; }
            public DateTime DateProcessed { get; set; }
        }
    }

    public static class Projections
    {
        /// <summary>
        /// A projection which uses multiple streams and manages several document types: main Read Model it's builiding and
        /// a side-readmodel used as a kind of helper
        /// </summary>
        public class OrderProjection: IInlineProjection
        {
            public class CompanySideReadModel
            {
                public Guid Id { get; set; }
                public string Name { get; set; }
            }

            private void When(IDocumentSession session, Events.OrderPlaced created)
            {
                Assert.Null(session.Load<ReadModels.Order>(created.Id));
                var company = session.Load<CompanySideReadModel>(created.CompanyId);
                session.Store(new ReadModels.Order()
                {
                    Id = created.Id,
                    CompanyName = company?.Name,
                    TotalAmount = created.TotalAmount,
                    CompanyId = created.CompanyId,
                    DateProcessed = DateTime.UtcNow
                });
            }

            private void When(IDocumentSession session, Events.CompanyCreated created)
            {
                session.Store(new CompanySideReadModel()
                {
                    Id = created.Id,
                    Name = created.Name
                });
            }

            private void When(IDocumentSession session, Events.CompanyNameChanged changed)
            {
                // replace with Patch once https://github.com/JasperFx/marten/pull/926 is in
                var company = session.Load<CompanySideReadModel>(changed.Id);
                company.Name = changed.NewName;
                session.Store(company);

                session.Patch<ReadModels.Order>(x => x.CompanyId == changed.Id)
                    .Set(x => x.CompanyName, changed.NewName);

                session.Patch<ReadModels.Order>(x => x.CompanyId == changed.Id)
                    .Set(x => x.DateProcessed, DateTime.UtcNow);
            }

            #region Infrastructure and dispatching

            public void Apply(IDocumentSession session, IReadOnlyList<StreamAction> streams)
            {
                ApplyAsync(session, streams, CancellationToken.None).GetAwaiter().GetResult();
            }

            public async Task ApplyAsync(IDocumentSession session, IReadOnlyList<StreamAction> streams, CancellationToken cancellation)
            {
                foreach (var @event in streams.SelectMany(x => x.Events))
                {
                    switch (@event.Data)
                    {
                        case Events.CompanyCreated created:
                            When(session, created);
                            break;

                        case Events.OrderPlaced placed:
                            When(session, placed);
                            break;

                        case Events.CompanyNameChanged changed:
                            When(session, changed);
                            break;
                    }
                    await session.SaveChangesAsync(cancellation);
                }
            }

            #endregion Infrastructure and dispatching
        }
    }

    public class MultidocumentProjectionTests: IntegrationContext
    {
        private static readonly Guid Company1Id = new Guid("5713D147-8D8E-499A-8CDF-ECEFF867D810");
        private static readonly Guid Company2Id = new Guid("18F5DE28-6027-4638-9D4F-496A5F29FB22");

        private static readonly Guid Order1Id = new Guid("C7F3F4B6-EDA9-4C5B-A0CF-6AE15EB83DEA");
        private static readonly Guid Order2Id = new Guid("367C1343-3A03-4888-A706-CA237B3CA020");
        private static readonly Guid Order3Id = new Guid("C3B5A850-92A1-449C-8653-B51BB56C84A3");

        [Theory]
        [InlineData(TenancyStyle.Single)]
        [InlineData(TenancyStyle.Conjoined)]
        public async Task Build_Projection_From_Stream(TenancyStyle tenancyStyle)
        {
            StoreOptions(_ =>
            {
                _.Events.V4Projections.Async(new Projections.OrderProjection());
                _.Events.TenancyStyle = tenancyStyle;
            });

            var daemon = theStore.BuildProjectionDaemon(logger: new DebugDaemonLogger());
            daemon.StartAll();

            await PublishEvents();
            await daemon.WaitForNonStaleResults();

            using (var session = theStore.OpenSession())
            {
                var order1 = session.Load<ReadModels.Order>(Order1Id);
                var order2 = session.Load<ReadModels.Order>(Order2Id);
                var order3 = session.Load<ReadModels.Order>(Order3Id);

                order1.CompanyName.ShouldBe("Mexico Railways");
                order2.CompanyName.ShouldBe("Mexico Railways");

                order3.CompanyName.ShouldBe("Microsoft");
            }
        }

        [Fact]
        public async Task Rebuild_LazyProjection_From_Stream()
        {
            StoreOptions(cfg =>
            {
                cfg.Events.V4Projections.Async(new Projections.OrderProjection());
            });
            // 1. publish events
            await PublishEvents();

            // 2. process them initially with daemon
            using (var daemon = theStore.BuildProjectionDaemon(logger: new DebugDaemonLogger()))
            {
                daemon.StartAll();

                await daemon.WaitForNonStaleResults();
                await daemon.StopAll();
            }

            // daemon stopped, now rebuild them with a new one
            var dt = DateTime.UtcNow;

            throw new NotImplementedException("REDO");
            // using (var daemon2 = theStore.BuildProjectionDaemon(logger: new DebugDaemonLogger(), projections: theStore.Events.AsyncProjections.ToArray()))
            // {
            //     await daemon2.RebuildAll(new CancellationTokenSource(10 * 1000).Token);
            //     await daemon2.WaitForNonStaleResults(new CancellationTokenSource(10 * 1000).Token);
            // }

            using (var session = theStore.OpenSession())
            {
                var order1 = session.Load<ReadModels.Order>(Order1Id);

                order1.CompanyName.ShouldBe("Mexico Railways");

                order1.DateProcessed.ShouldBeGreaterThanOrEqualTo(dt);
            }
        }

        [Fact]
        public async Task Rebuilding_Projection_Removes_Old_State()
        {
            StoreOptions(cfg =>
            {
                cfg.Events.V4Projections.Async(new Projections.OrderProjection());
            });

            var daemon = theStore.BuildProjectionDaemon(logger: new DebugDaemonLogger());
            daemon.StartAll();

            await PublishEvents();
            await daemon.WaitForNonStaleResults();

            using (var session = theStore.OpenSession())
            {
                session.Patch<ReadModels.Order>(Order1Id).Set(x => x.CompanyName, "CorruptedValue");
                await session.SaveChangesAsync();
            }

            ReadModels.Order order1;
            using (var session = theStore.OpenSession())
            {
                order1 = session.Load<ReadModels.Order>(Order1Id);
                order1.CompanyName.ShouldBe("CorruptedValue");
            }
            await daemon.StopAll();

            await daemon.RebuildAll();
            await daemon.WaitForNonStaleResults();
            using (var session = theStore.OpenSession())
            {
                var order2 = session.Load<ReadModels.Order>(Order1Id);
                order2.CompanyName.ShouldBe("Mexico Railways");
                order2.ShouldNotBeTheSameAs(order1);
            }
        }

        private async Task PublishEvents()
        {
            using (var sess = theStore.OpenSession())
            {
                foreach (var @event in GetEvents())
                {
                    var id = (Guid)@event.GetType().GetTypeInfo().GetProperty("Id").GetValue(@event);
                    sess.Events.Append(id, @event);
                    await sess.SaveChangesAsync();
                }
            }
        }

        private IEnumerable<object> GetEvents() => new object[]
        {
            new Events.CompanyCreated()
            {
                Id = Company1Id,
                Name = "Mexico Railways LTD",
                Address = "Los padres, Canon Diablo",
                TaxpayerId = "123-456"
            },
            new Events.OrderPlaced() {Id = Order1Id, CompanyId = Company1Id, TotalAmount = 55.65m},
            new Events.CompanyNameChanged() {Id = Company1Id, NewName = "Mexico Railways"},
            new Events.OrderPlaced() {Id = Order2Id, CompanyId = Company1Id, TotalAmount = 155.65m},

            new Events.CompanyCreated()
            {
                Id = Company2Id,
                Name = "Microsoft",
                Address = "Sillicon Valley",
                TaxpayerId = "567-928"
            },
            new Events.OrderPlaced() {Id = Order3Id, CompanyId = Company2Id, TotalAmount = 11.11m}
        };

        public MultidocumentProjectionTests(DefaultStoreFixture fixture) : base(fixture)
        {
            DocumentTracking = DocumentTracking.IdentityOnly;
        }
    }
}
