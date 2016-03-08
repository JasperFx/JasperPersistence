﻿using System;
using Marten.Services;
using Marten.Testing.Documents;
using Shouldly;
using Xunit;

namespace Marten.Testing.Session
{
    public class document_session_find_json_Tests : DocumentSessionFixture<NulloIdentityMap>
    {
        // SAMPLE: find-json-by-id
        [Fact]
        public void when_find_then_a_json_should_be_returned()
        {
            var issue = new Issue { Title = "Issue 1" };

            theSession.Store(issue);
            theSession.SaveChanges();

            var json = theSession.FindJsonById<Issue>(issue.Id);
            json.ShouldBe($"{{\"Id\": \"{issue.Id}\", \"Title\": \"Issue 1\", \"AssigneeId\": null}}");
        }
        // ENDSAMPLE

        [Fact]
        public void when_find_then_a_null_should_be_returned()
        {
            var json = theSession.FindJsonById<Issue>(Guid.NewGuid());
            json.ShouldBeNull();
        }
    }
}