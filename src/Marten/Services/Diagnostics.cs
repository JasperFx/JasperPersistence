#nullable enable
using System;
using Marten.Internal.Sessions;
using Marten.Linq;
using Npgsql;
using Weasel.Postgresql;

namespace Marten.Services;

public class Diagnostics: IDiagnostics
{
    private readonly DocumentStore _store;
    private Version? _postgreSqlVersion;

    public Diagnostics(DocumentStore store)
    {
        _store = store;
    }

    /// <summary>
    ///     Preview the database command that will be executed for this compiled query
    ///     object
    /// </summary>
    /// <typeparam name="TDoc"></typeparam>
    /// <typeparam name="TReturn"></typeparam>
    /// <param name="query"></param>
    /// <returns></returns>
    public NpgsqlCommand PreviewCommand<TDoc, TReturn>(ICompiledQuery<TDoc, TReturn> query,
        DocumentTracking trackingMode = DocumentTracking.QueryOnly)
    {
        using var session = OpenQuerySession(trackingMode);
        var source = _store.GetCompiledQuerySourceFor(query, session);
        var handler = source.Build(query, session);

        var command = new NpgsqlCommand();
        var builder = new CommandBuilder(command);
        handler.ConfigureCommand(builder, session);

        command.CommandText = builder.ToString();

        return command;
    }

    /// <summary>
    ///     Find the Postgresql EXPLAIN PLAN for this compiled query
    /// </summary>
    /// <typeparam name="TDoc"></typeparam>
    /// <typeparam name="TReturn"></typeparam>
    /// <param name="query"></param>
    /// <returns></returns>
    public QueryPlan ExplainPlan<TDoc, TReturn>(ICompiledQuery<TDoc, TReturn> query)
    {
        var cmd = PreviewCommand(query);

        using var conn = _store.Tenancy.Default.Database.CreateConnection();
        conn.Open();
        return conn.ExplainQuery(_store.Serializer, cmd)!;
    }

    /// <summary>
    ///     Method to fetch Postgres server version
    /// </summary>
    /// <returns>Returns version</returns>
    public Version GetPostgresVersion()
    {
        if (_postgreSqlVersion != null)
        {
            return _postgreSqlVersion;
        }

        using var conn = _store.Tenancy.Default.Database.CreateConnection();
        conn.Open();

        _postgreSqlVersion = conn.PostgreSqlVersion;

        return _postgreSqlVersion;
    }

    private QuerySession OpenQuerySession(DocumentTracking tracking)
    {
        switch (tracking)
        {
            case DocumentTracking.None:
                return (QuerySession)_store.LightweightSession();
            case DocumentTracking.QueryOnly:
                return (QuerySession)_store.QuerySession();
            case DocumentTracking.IdentityOnly:
                return (QuerySession)_store.OpenSession();
            case DocumentTracking.DirtyTracking:
                return (QuerySession)_store.DirtyTrackedSession();
        }

        throw new ArgumentOutOfRangeException(nameof(tracking));
    }
}
