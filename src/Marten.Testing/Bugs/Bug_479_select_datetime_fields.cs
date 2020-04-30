using System;
using System.Linq;
using Baseline.Dates;
using Marten.Testing.Harness;
using Shouldly;
using Xunit;

namespace Marten.Testing.Bugs
{
    public class Bug_479_select_datetime_fields: IntegrationContext
    {
        public Bug_479_select_datetime_fields(DefaultStoreFixture fixture) : base(fixture)
        {
            StoreOptions(_ => _.UseDefaultSerialization(EnumStorage.AsString));
        }

        [Fact]
        public void select_date_time_as_utc()
        {
            var doc = new DocWithDates
            {
                DateTime = DateTime.Today.ToUniversalTime()
            };

            using (var session = theStore.OpenSession())
            {
                session.Store(doc);
                session.SaveChanges();

                var date = session.Query<DocWithDates>().Where(x => x.Id == doc.Id).Select(x => new { Date = x.DateTime }).Single();

                date.Date.ShouldBe(doc.DateTime);
            }
        }

        [Fact]
        public void select_date_time_offset()
        {
            var doc = new DocWithDates
            {
                DateTimeOffset = new DateTimeOffset(2016, 8, 15, 18, 0, 0, 0.Hours())
            };

            using (var session = theStore.OpenSession())
            {
                session.Store(doc);
                session.SaveChanges();

                var date = session.Query<DocWithDates>().Where(x => x.Id == doc.Id).Select(x => new { Date = x.DateTimeOffset }).Single();

                date.Date.ShouldBe(doc.DateTimeOffset);
            }
        }

        public class DocWithDates
        {
            public Guid Id = Guid.NewGuid();

            public DateTime DateTime;
            public DateTimeOffset DateTimeOffset;

            public DateTime? NullableDateTime;
            public DateTimeOffset? NullableDateTimeOffset;
        }
    }
}
