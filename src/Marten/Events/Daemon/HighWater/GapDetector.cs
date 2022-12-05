using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Marten.Services;
using Npgsql;
using Weasel.Postgresql;

namespace Marten.Events.Daemon.HighWater;

internal class GapDetector: ISingleQueryHandler<long?>
{
    private readonly NpgsqlCommand _gapDetection;
    private readonly NpgsqlParameter _start;

    public GapDetector(EventGraph graph)
    {
        _gapDetection = new NpgsqlCommand($@"
select seq_id
from   (select
               seq_id,
               lead(seq_id)
               over (order by seq_id) as no
        from
               {graph.DatabaseSchemaName}.mt_events where seq_id >= :start) ct
where  no is not null
  and    no - seq_id > 1
LIMIT 1;
select max(seq_id) from {graph.DatabaseSchemaName}.mt_events where seq_id >= :start;
".Trim());

        _start = _gapDetection.AddNamedParameter("start", 0L);
    }

    public long Start
    {
        set => _start.Value = value;
    }

    public NpgsqlCommand BuildCommand()
    {
        return _gapDetection;
    }

    public async Task<long?> HandleAsync(DbDataReader reader, CancellationToken token)
    {
        // If there is a row, this tells us the first sequence gap
        if (await reader.ReadAsync(token).ConfigureAwait(false))
        {
            return await reader.GetFieldValueAsync<long>(0, token).ConfigureAwait(false);
        }

        // use the latest sequence in the event table because there is NO gap
        await reader.NextResultAsync(token).ConfigureAwait(false);
        if (!await reader.ReadAsync(token).ConfigureAwait(false))
        {
            return null;
        }

        if (!await reader.IsDBNullAsync(0, token).ConfigureAwait(false))
        {
            return await reader.GetFieldValueAsync<long>(0, token).ConfigureAwait(false);
        }

        return null;
    }
}
