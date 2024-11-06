using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using JasperFx.Events;
using Marten.Events.Daemon.Internals;
using Marten.Internal;
using Marten.Internal.Operations;
using NpgsqlTypes;
using Weasel.Core.Operations;
using Weasel.Postgresql;

namespace Marten.Events.Daemon.Progress;

internal class InsertProjectionProgress: IStorageOperation
{
    private readonly EventGraph _events;
    private readonly EventRange _progress;

    public InsertProjectionProgress(EventGraph events, EventRange progress)
    {
        _events = events;
        _progress = progress;
    }

    public void ConfigureCommand(ICommandBuilder builder, IStorageSession session)
    {
        var parameters =
            builder.AppendWithParameters($"insert into {_events.ProgressionTable} (name, last_seq_id) values (?, ?)");

        parameters[0].Value = _progress.ShardName.Identity;
        parameters[1].Value = _progress.SequenceCeiling;
        parameters[1].DbType = DbType.Int64;
    }

    public Type DocumentType => typeof(IEvent);

    public void Postprocess(DbDataReader reader, IList<Exception> exceptions)
    {
        // Nothing
    }

    public Task PostprocessAsync(DbDataReader reader, IList<Exception> exceptions, CancellationToken token)
    {
        return Task.CompletedTask;
    }

    public OperationRole Role()
    {
        return OperationRole.Events;
    }
}
