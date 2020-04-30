﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Baseline;
using Marten.Services;
using Marten.Testing.Documents;
using Marten.Testing.Harness;
using Xunit;

namespace Marten.Testing.Linq
{
    [ControlledQueryStoryteller]
    public class query_against_dateoffset_Tests : IntegrationContextWithIdentityMap<NulloIdentityMap>
    {
        [Fact]
        public void query()
        {
            theSession.Store(new Target{Number = 1, DateOffset = DateTimeOffset.Now.AddMinutes(30)});
            theSession.Store(new Target{Number = 2, DateOffset = DateTimeOffset.Now.AddDays(1)});
            theSession.Store(new Target{Number = 3, DateOffset = DateTimeOffset.Now.AddHours(1)});
            theSession.Store(new Target{Number = 4, DateOffset = DateTimeOffset.Now.AddHours(-2)});
            theSession.Store(new Target{Number = 5, DateOffset = DateTimeOffset.Now.AddHours(-3)});

            theSession.SaveChanges();

            theSession.Query<Target>().Where(x => x.DateOffset > DateTimeOffset.Now).ToArray()
                .Select(x => x.Number)
                .ShouldHaveTheSameElementsAs(1, 2, 3);
        }

        [Fact]
        public void can_index_against_datetime_offset()
        {
            StoreOptions(_ =>
            {
                _.Schema.For<Target>().Index(x => x.DateOffset);
            });

            theSession.Store(new Target { Number = 1, DateOffset = DateTimeOffset.Now.AddMinutes(30) });
            theSession.Store(new Target { Number = 2, DateOffset = DateTimeOffset.Now.AddDays(1) });
            theSession.Store(new Target { Number = 3, DateOffset = DateTimeOffset.Now.AddHours(1) });
            theSession.Store(new Target { Number = 4, DateOffset = DateTimeOffset.Now.AddHours(-2) });
            theSession.Store(new Target { Number = 5, DateOffset = DateTimeOffset.Now.AddHours(-3) });

            theSession.SaveChanges();


            theSession.Query<Target>().Where(x => x.DateOffset > DateTimeOffset.Now).OrderBy(x => x.DateOffset).ToArray()
                .Select(x => x.Number)
                .ShouldHaveTheSameElementsAs(1, 3, 2);
        }

        public query_against_dateoffset_Tests(DefaultStoreFixture fixture) : base(fixture)
        {
        }
    }


}
