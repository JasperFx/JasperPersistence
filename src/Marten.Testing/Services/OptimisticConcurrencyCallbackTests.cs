﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Marten.Services;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Marten.Testing.Services
{
    public class OptimisticConcurrencyCallbackTests
    {
        private readonly long theNewVersion = 2;
        private readonly long theOldVersion = 1;
        private readonly VersionTracker theVersionTracker = new VersionTracker();
        private readonly string theId = "foo";
        private OptimisticConcurrencyCallback<Target> theCallback;

        public OptimisticConcurrencyCallbackTests()
        {
            theCallback = new OptimisticConcurrencyCallback<Target>(theId, theVersionTracker);
        }

        [Fact]
        public void postprocess_sync_hit()
        {
            var reader = Substitute.For<DbDataReader>();
            reader.Read().Returns(true);
            reader.GetFieldValue<long>(0).Returns(theNewVersion);

            var exceptions = new List<Exception>();
            theCallback.Postprocess(reader, exceptions);

            exceptions.Any().ShouldBeFalse();

            theVersionTracker.Version<Target>(theId).ShouldBe(theNewVersion);
        }

        [Fact]
        public void postprocess_sync_miss()
        {
            var reader = Substitute.For<DbDataReader>();
            reader.Read().Returns(true);
            reader.GetFieldValue<long>(0).Returns(-1);

            var exceptions = new List<Exception>();
            theCallback.Postprocess(reader, exceptions);

            exceptions.Single().ShouldBeOfType<ConcurrencyException>();
        }

        [Fact]
        public async Task postprocess_async_hit()
        {
            var token = new CancellationToken();

            var reader = Substitute.For<DbDataReader>();
            reader.ReadAsync(token).Returns(Task.FromResult(true));
            reader.GetFieldValueAsync<long>(0, token).Returns(Task.FromResult(theNewVersion));

            var exceptions = new List<Exception>();
            await theCallback.PostprocessAsync(reader, exceptions, token);

            exceptions.Any().ShouldBeFalse();

            theVersionTracker.Version<Target>(theId).ShouldBe(theNewVersion);
        }


        [Fact]
        public async Task postprocess_async_miss()
        {
            var token = new CancellationToken();

            var reader = Substitute.For<DbDataReader>();
            reader.ReadAsync(token).Returns(Task.FromResult(true));
            reader.GetFieldValueAsync<long>(0, token).Returns(Task.FromResult(-1L));

            var exceptions = new List<Exception>();
            await theCallback.PostprocessAsync(reader, exceptions, token);

            exceptions.Single().ShouldBeOfType<ConcurrencyException>();
        }

    }
}