using System;
using System.Net;
using Marten.Internal;
using Marten.Internal.Sessions;
using Marten.Linq;
using Weasel.Postgresql;
using Marten.Util;
using Npgsql;
#nullable enable
namespace Marten.Services
{
    public class Diagnostics: IDiagnostics
    {
        private readonly DocumentStore _store;
        private Version? _postgreSqlVersion;

        public Diagnostics(DocumentStore store)
        {
            _store = store;
        }

        /// <summary>
        /// Preview the database command that will be executed for this compiled query
        /// object
        /// </summary>
        /// <typeparam name="TDoc"></typeparam>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        public NpgsqlCommand PreviewCommand<TDoc, TReturn>(ICompiledQuery<TDoc, TReturn> query)
        {
            using var session = _store.QuerySession();
            var source = _store.Options.GetCompiledQuerySourceFor(query, (QuerySession)session);
            var handler = source.Build(query, (IMartenSession) session);

            var command = new NpgsqlCommand();
            var builder = new CommandBuilder(command);
            handler.ConfigureCommand(builder, (IMartenSession) session);

            command.CommandText = builder.ToString();

            return command;
        }

        /// <summary>
        /// Find the Postgresql EXPLAIN PLAN for this compiled query
        /// </summary>
        /// <typeparam name="TDoc"></typeparam>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        public QueryPlan ExplainPlan<TDoc, TReturn>(ICompiledQuery<TDoc, TReturn> query)
        {
            var cmd = PreviewCommand(query);

            using var conn = _store.Tenancy.Default.Storage.CreateConnection();
            conn.Open();
            return conn.ExplainQuery(_store.Serializer, cmd)!;
        }

        /// <summary>
        /// Method to fetch Postgres server version
        /// </summary>
        /// <returns>Returns version</returns>
        public Version GetPostgresVersion()
        {
            if (_postgreSqlVersion != null)
                return _postgreSqlVersion;

            using var conn = _store.Tenancy.Default.Storage.CreateConnection();
            conn.Open();

            _postgreSqlVersion = conn.PostgreSqlVersion;

            return _postgreSqlVersion;
        }
    }
}
