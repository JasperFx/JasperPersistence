﻿using Shouldly;
using Xunit;

namespace Marten.Testing.Patching
{
    public class can_build_the_patching_function : IntegratedFixture
    {
        [Fact]
        public void does_not_blow_up()
        {
            var transform = theStore.DefaultTenant.TransformFor("patch_doc");

            theStore.Schema.DbObjects.Functions()
                .ShouldContain(transform.Identifier);
        }
    }
}