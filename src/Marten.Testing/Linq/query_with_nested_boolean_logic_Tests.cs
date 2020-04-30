﻿using System;
using System.Collections.Generic;
using System.Linq;
using Marten.Services;
using Marten.Testing.Documents;
using Marten.Testing.Harness;
using Shouldly;
using Xunit;

namespace Marten.Testing.Linq
{
    [ControlledQueryStoryteller]
    public class query_with_nested_boolean_logic_Tests : IntegrationContextWithIdentityMap<NulloIdentityMap>
    {
        [Fact]
        public void TestModalOrQuery()
        {
            var target1 = new Target { String = "Bert", Date = new DateTime(2016, 03, 10) };
            var target2 = new Target { String = null, Date = new DateTime(2016, 03, 10) };

            theSession.Store(target1, target2);
            theSession.SaveChanges();

            var startDate = new DateTime(2016, 03, 01);
            var endDate = new DateTime(2016, 04, 01);

            var query = theSession.Query<Target>().Where(item => (item.String != null && item.Date >= startDate && item.Date <= endDate)
                || (item.String == null && item.Date >= startDate && item.Date <= endDate));

            query.ToList().Count.ShouldBeGreaterThanOrEqualTo(2);

        }

        public query_with_nested_boolean_logic_Tests(DefaultStoreFixture fixture) : base(fixture)
        {
        }
    }
}
