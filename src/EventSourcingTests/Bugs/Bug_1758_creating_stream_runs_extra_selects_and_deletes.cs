using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using JasperFx.Events.Projections;
using Marten;
using Marten.Events.Projections;
using Marten.Services;
using Marten.Testing.Harness;
using Npgsql;
using Shouldly;
using Weasel.Core.Operations;
using Xunit;

namespace EventSourcingTests.Bugs;

public class Bug_1758_creating_stream_runs_extra_selects_and_deletes : BugIntegrationContext
{
    [Fact]
    public async Task should_not_run_selects_and_deletes_to_non_affected_aggregates()
    {
        var logger = new CollectingLogger();

        using var documentStore = SeparateStore(x =>
        {
            x.Projections.Snapshot<AggregateA>(ProjectionLifecycle.Inline);
            x.Projections.Snapshot<AggregateB>(ProjectionLifecycle.Inline);
            x.Projections.Snapshot<AggregateC>(ProjectionLifecycle.Inline);
            x.Logger(logger);
        });

        await documentStore.Advanced.Clean.CompletelyRemoveAllAsync();

        using var session = documentStore.LightweightSession();
        var id = session.Events.StartStream<AggregateA>(new CreateAEvent {Name = "Test"}).Id;
        await session.SaveChangesAsync();

        var commit = logger.LastCommit;

        commit.Deleted.Any().ShouldBeFalse();
    }

    private class CollectingLogger: IMartenLogger, IMartenSessionLogger
    {
        public IList<string> CommandTexts { get; } = new List<string>();

        public IMartenSessionLogger StartSession(IQuerySession session) => this;

        public void SchemaChange(string sql)
        {
        }

        public void LogSuccess(DbCommand command) => CommandTexts.Add(command.CommandText);
        public void LogFailure(DbCommand command, Exception ex) => CommandTexts.Add(command.CommandText);
        public void LogSuccess(DbBatch batch)
        {

        }

        public void LogFailure(DbBatch batch, Exception ex)
        {

        }

        public void LogFailure(Exception ex, string message)
        {

        }

        public void RecordSavedChanges(IDocumentSession session, IChangeSet commit)
        {
            LastCommit = commit;
        }

        public IChangeSet LastCommit { get; set; }

        public void OnBeforeExecute(DbCommand command)
        {
        }

        public void OnBeforeExecute(DbBatch batch)
        {

        }
    }
}

public class CreateAEvent { public string Name { get; set; } }
public class UpdateAEvent { public string NewName { get; set; } }

public class CreateBEvent { public string Name { get; set; } }
public class CreateCEvent { public string Name { get; set; } }

public class AggregateA
{
    public Guid Id { get; set; }
    public string Name { get; set; }

    public void Apply(CreateAEvent create) => Name = create.Name;
    public void Apply(UpdateAEvent update) => Name = update.NewName;
}

public class AggregateB
{
    public Guid Id { get; set; }
    public string Name { get; set; }

    public void Apply(CreateBEvent create) => Name = create.Name;
}

public class AggregateC
{
    public Guid Id { get; set; }
    public string Name { get; set; }

    public void Apply(CreateCEvent create) => Name = create.Name;
}
