﻿using System.Linq;
using System.Threading.Tasks;
using Marten.Services;
using Marten.Testing.Fixtures;
using Shouldly;
using Xunit;

namespace Marten.Testing
{
    [Collection("DefaultSchema")]
    public class query_scalar_values_with_select_in_query_async : DocumentSessionFixture<NulloIdentityMap>
    {
        [Fact]
        public async Task get_sum_of_integers_asynchronously()
        {
            theSession.Store(new Target { Color = Colors.Blue, Number = 1 });
            theSession.Store(new Target { Color = Colors.Red, Number = 2 });
            theSession.Store(new Target { Color = Colors.Green, Number = 3 });
            theSession.Store(new Target { Color = Colors.Blue, Number = 4 });

            theSession.SaveChanges();
            var sumResults = await theSession.QueryAsync<int>("select sum(CAST(d.data ->> 'Number' as integer)) as number from mt_doc_target as d");
            var sum = sumResults.Single();
            sum.ShouldBe(10);
        }

        [Fact]
        public async Task get_count_asynchronously()
        {
            theSession.Store(new Target { Color = Colors.Blue, Number = 1 });
            theSession.Store(new Target { Color = Colors.Red, Number = 2 });
            theSession.Store(new Target { Color = Colors.Green, Number = 3 });
            theSession.Store(new Target { Color = Colors.Blue, Number = 4 });

            theSession.SaveChanges();
            var sumResults = await theSession.QueryAsync<int>("select count(*) from mt_doc_target");
            var sum = sumResults.Single();
            sum.ShouldBe(4);
        }
    }
}