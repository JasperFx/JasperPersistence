﻿using System.Linq;
using Marten;
using Marten.Services;
using Marten.Testing.Fixtures;
using Shouldly;
using Xunit;

namespace Marten.Testing.Linq
{
    public class query_with_is_one_of_Tests : DocumentSessionFixture<NulloIdentityMap>
    {
        [Fact]
        public void can_query_against_integers()
        {
            var targets = Target.GenerateRandomData(100).ToArray();
            theStore.BulkInsert(targets);

            var validNumbers = targets.Select(x => x.Number).Distinct().Take(3).ToArray();

            var found = theSession.Query<Target>().Where(x => x.Number.IsOneOf(validNumbers)).ToArray();

            found.Count().ShouldBeLessThan(100);

            var expected = targets
                .Where(x => validNumbers
                .Contains(x.Number))
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToArray();

            found.OrderBy(x => x.Id).Select(x => x.Id)
                .ShouldHaveTheSameElementsAs(expected);
        }
    }
}