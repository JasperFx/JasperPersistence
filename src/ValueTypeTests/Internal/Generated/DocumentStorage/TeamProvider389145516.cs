// <auto-generated/>
#pragma warning disable
using Marten.Internal;
using Marten.Internal.Storage;
using Marten.Schema;
using Marten.Schema.Arguments;
using Npgsql;
using System.Collections.Generic;
using ValueTypeTests.VogenIds;
using Weasel.Core;
using Weasel.Postgresql;
using TeamId = ValueTypeTests.VogenIds.TeamId;

namespace Marten.Generated.DocumentStorage
{
    // START: UpsertTeamOperation389145516
    public class UpsertTeamOperation389145516 : Marten.Internal.Operations.StorageOperation<Team, TeamId>
    {
        private readonly Team _document;
        private readonly TeamId _id;
        private readonly System.Collections.Generic.Dictionary<TeamId, System.Guid> _versions;
        private readonly Marten.Schema.DocumentMapping _mapping;

        public UpsertTeamOperation389145516(Team document, TeamId id, System.Collections.Generic.Dictionary<TeamId, System.Guid> versions, Marten.Schema.DocumentMapping mapping) : base(document, id, versions, mapping)
        {
            _document = document;
            _id = id;
            _versions = versions;
            _mapping = mapping;
        }



        public override void Postprocess(System.Data.Common.DbDataReader reader, System.Collections.Generic.IList<System.Exception> exceptions)
        {
            storeVersion();
        }


        public override System.Threading.Tasks.Task PostprocessAsync(System.Data.Common.DbDataReader reader, System.Collections.Generic.IList<System.Exception> exceptions, System.Threading.CancellationToken token)
        {
            storeVersion();
            // Nothing
            return System.Threading.Tasks.Task.CompletedTask;
        }


        public override Marten.Internal.Operations.OperationRole Role()
        {
            return Marten.Internal.Operations.OperationRole.Upsert;
        }


        public override NpgsqlTypes.NpgsqlDbType DbType()
        {
            return NpgsqlTypes.NpgsqlDbType.Text;
        }


        public override void ConfigureParameters(Weasel.Postgresql.IGroupedParameterBuilder parameterBuilder, Weasel.Postgresql.ICommandBuilder builder, Team document, Marten.Internal.IMartenSession session)
        {
            builder.Append("select strong_typed8.mt_upsert_team(");
            var parameter0 = parameterBuilder.AppendParameter(session.Serializer.ToJson(_document));
            parameter0.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;
            // .Net Class Type
            var parameter1 = parameterBuilder.AppendParameter(_document.GetType().FullName);
            parameter1.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar;

            if (document.Id != null)
            {
                var parameter2 = parameterBuilder.AppendParameter(document.Id.Value.Value);
                parameter2.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text;
            }

            else
            {
                var parameter2 = parameterBuilder.AppendParameter<object>(System.DBNull.Value);
            }

            setVersionParameter(parameterBuilder);
            builder.Append(')');
        }

    }

    // END: UpsertTeamOperation389145516


    // START: InsertTeamOperation389145516
    public class InsertTeamOperation389145516 : Marten.Internal.Operations.StorageOperation<Team, TeamId>
    {
        private readonly Team _document;
        private readonly TeamId _id;
        private readonly System.Collections.Generic.Dictionary<TeamId, System.Guid> _versions;
        private readonly Marten.Schema.DocumentMapping _mapping;

        public InsertTeamOperation389145516(Team document, TeamId id, System.Collections.Generic.Dictionary<TeamId, System.Guid> versions, Marten.Schema.DocumentMapping mapping) : base(document, id, versions, mapping)
        {
            _document = document;
            _id = id;
            _versions = versions;
            _mapping = mapping;
        }



        public override void Postprocess(System.Data.Common.DbDataReader reader, System.Collections.Generic.IList<System.Exception> exceptions)
        {
            storeVersion();
        }


        public override System.Threading.Tasks.Task PostprocessAsync(System.Data.Common.DbDataReader reader, System.Collections.Generic.IList<System.Exception> exceptions, System.Threading.CancellationToken token)
        {
            storeVersion();
            // Nothing
            return System.Threading.Tasks.Task.CompletedTask;
        }


        public override Marten.Internal.Operations.OperationRole Role()
        {
            return Marten.Internal.Operations.OperationRole.Insert;
        }


        public override NpgsqlTypes.NpgsqlDbType DbType()
        {
            return NpgsqlTypes.NpgsqlDbType.Text;
        }


        public override void ConfigureParameters(Weasel.Postgresql.IGroupedParameterBuilder parameterBuilder, Weasel.Postgresql.ICommandBuilder builder, Team document, Marten.Internal.IMartenSession session)
        {
            builder.Append("select strong_typed8.mt_insert_team(");
            var parameter0 = parameterBuilder.AppendParameter(session.Serializer.ToJson(_document));
            parameter0.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;
            // .Net Class Type
            var parameter1 = parameterBuilder.AppendParameter(_document.GetType().FullName);
            parameter1.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar;

            if (document.Id != null)
            {
                var parameter2 = parameterBuilder.AppendParameter(document.Id.Value.Value);
                parameter2.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text;
            }

            else
            {
                var parameter2 = parameterBuilder.AppendParameter<object>(System.DBNull.Value);
            }

            setVersionParameter(parameterBuilder);
            builder.Append(')');
        }

    }

    // END: InsertTeamOperation389145516


    // START: UpdateTeamOperation389145516
    public class UpdateTeamOperation389145516 : Marten.Internal.Operations.StorageOperation<Team, TeamId>
    {
        private readonly Team _document;
        private readonly TeamId _id;
        private readonly System.Collections.Generic.Dictionary<TeamId, System.Guid> _versions;
        private readonly Marten.Schema.DocumentMapping _mapping;

        public UpdateTeamOperation389145516(Team document, TeamId id, System.Collections.Generic.Dictionary<TeamId, System.Guid> versions, Marten.Schema.DocumentMapping mapping) : base(document, id, versions, mapping)
        {
            _document = document;
            _id = id;
            _versions = versions;
            _mapping = mapping;
        }



        public override void Postprocess(System.Data.Common.DbDataReader reader, System.Collections.Generic.IList<System.Exception> exceptions)
        {
            storeVersion();
            postprocessUpdate(reader, exceptions);
        }


        public override async System.Threading.Tasks.Task PostprocessAsync(System.Data.Common.DbDataReader reader, System.Collections.Generic.IList<System.Exception> exceptions, System.Threading.CancellationToken token)
        {
            storeVersion();
            await postprocessUpdateAsync(reader, exceptions, token);
        }


        public override Marten.Internal.Operations.OperationRole Role()
        {
            return Marten.Internal.Operations.OperationRole.Update;
        }


        public override NpgsqlTypes.NpgsqlDbType DbType()
        {
            return NpgsqlTypes.NpgsqlDbType.Text;
        }


        public override void ConfigureParameters(Weasel.Postgresql.IGroupedParameterBuilder parameterBuilder, Weasel.Postgresql.ICommandBuilder builder, Team document, Marten.Internal.IMartenSession session)
        {
            builder.Append("select strong_typed8.mt_update_team(");
            var parameter0 = parameterBuilder.AppendParameter(session.Serializer.ToJson(_document));
            parameter0.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;
            // .Net Class Type
            var parameter1 = parameterBuilder.AppendParameter(_document.GetType().FullName);
            parameter1.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar;

            if (document.Id != null)
            {
                var parameter2 = parameterBuilder.AppendParameter(document.Id.Value.Value);
                parameter2.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text;
            }

            else
            {
                var parameter2 = parameterBuilder.AppendParameter<object>(System.DBNull.Value);
            }

            setVersionParameter(parameterBuilder);
            builder.Append(')');
        }

    }

    // END: UpdateTeamOperation389145516


    // START: QueryOnlyTeamSelector389145516
    public class QueryOnlyTeamSelector389145516 : Marten.Internal.CodeGeneration.DocumentSelectorWithOnlySerializer, Marten.Linq.Selectors.ISelector<Team>
    {
        private readonly Marten.Internal.IMartenSession _session;
        private readonly Marten.Schema.DocumentMapping _mapping;

        public QueryOnlyTeamSelector389145516(Marten.Internal.IMartenSession session, Marten.Schema.DocumentMapping mapping) : base(session, mapping)
        {
            _session = session;
            _mapping = mapping;
        }



        public Team Resolve(System.Data.Common.DbDataReader reader)
        {

            Team document;
            document = _serializer.FromJson<Team>(reader, 0);
            return document;
        }


        public async System.Threading.Tasks.Task<Team> ResolveAsync(System.Data.Common.DbDataReader reader, System.Threading.CancellationToken token)
        {

            Team document;
            document = await _serializer.FromJsonAsync<Team>(reader, 0, token).ConfigureAwait(false);
            return document;
        }

    }

    // END: QueryOnlyTeamSelector389145516


    // START: LightweightTeamSelector389145516
    public class LightweightTeamSelector389145516 : Marten.Internal.CodeGeneration.DocumentSelectorWithVersions<Team, TeamId>, Marten.Linq.Selectors.ISelector<Team>
    {
        private readonly Marten.Internal.IMartenSession _session;
        private readonly Marten.Schema.DocumentMapping _mapping;

        public LightweightTeamSelector389145516(Marten.Internal.IMartenSession session, Marten.Schema.DocumentMapping mapping) : base(session, mapping)
        {
            _session = session;
            _mapping = mapping;
        }



        public Team Resolve(System.Data.Common.DbDataReader reader)
        {
            var id = TeamId.From(reader.GetFieldValue<string>(0));

            Team document;
            document = _serializer.FromJson<Team>(reader, 1);
            _session.MarkAsDocumentLoaded(id, document);
            return document;
        }


        public async System.Threading.Tasks.Task<Team> ResolveAsync(System.Data.Common.DbDataReader reader, System.Threading.CancellationToken token)
        {
            var id = TeamId.From(await reader.GetFieldValueAsync<string>(0, token));

            Team document;
            document = await _serializer.FromJsonAsync<Team>(reader, 1, token).ConfigureAwait(false);
            _session.MarkAsDocumentLoaded(id, document);
            return document;
        }

    }

    // END: LightweightTeamSelector389145516


    // START: IdentityMapTeamSelector389145516
    public class IdentityMapTeamSelector389145516 : Marten.Internal.CodeGeneration.DocumentSelectorWithIdentityMap<Team, TeamId>, Marten.Linq.Selectors.ISelector<Team>
    {
        private readonly Marten.Internal.IMartenSession _session;
        private readonly Marten.Schema.DocumentMapping _mapping;

        public IdentityMapTeamSelector389145516(Marten.Internal.IMartenSession session, Marten.Schema.DocumentMapping mapping) : base(session, mapping)
        {
            _session = session;
            _mapping = mapping;
        }



        public Team Resolve(System.Data.Common.DbDataReader reader)
        {
            var id = TeamId.From(reader.GetFieldValue<string>(0));
            if (_identityMap.TryGetValue(id, out var existing)) return existing;

            Team document;
            document = _serializer.FromJson<Team>(reader, 1);
            _session.MarkAsDocumentLoaded(id, document);
            _identityMap[id] = document;
            return document;
        }


        public async System.Threading.Tasks.Task<Team> ResolveAsync(System.Data.Common.DbDataReader reader, System.Threading.CancellationToken token)
        {
            var id = TeamId.From(await reader.GetFieldValueAsync<string>(0, token));
            if (_identityMap.TryGetValue(id, out var existing)) return existing;

            Team document;
            document = await _serializer.FromJsonAsync<Team>(reader, 1, token).ConfigureAwait(false);
            _session.MarkAsDocumentLoaded(id, document);
            _identityMap[id] = document;
            return document;
        }

    }

    // END: IdentityMapTeamSelector389145516


    // START: DirtyTrackingTeamSelector389145516
    public class DirtyTrackingTeamSelector389145516 : Marten.Internal.CodeGeneration.DocumentSelectorWithDirtyChecking<Team, TeamId>, Marten.Linq.Selectors.ISelector<Team>
    {
        private readonly Marten.Internal.IMartenSession _session;
        private readonly Marten.Schema.DocumentMapping _mapping;

        public DirtyTrackingTeamSelector389145516(Marten.Internal.IMartenSession session, Marten.Schema.DocumentMapping mapping) : base(session, mapping)
        {
            _session = session;
            _mapping = mapping;
        }



        public Team Resolve(System.Data.Common.DbDataReader reader)
        {
            var id = TeamId.From(reader.GetFieldValue<string>(0));
            if (_identityMap.TryGetValue(id, out var existing)) return existing;

            Team document;
            document = _serializer.FromJson<Team>(reader, 1);
            _session.MarkAsDocumentLoaded(id, document);
            _identityMap[id] = document;
            StoreTracker(_session, document);
            return document;
        }


        public async System.Threading.Tasks.Task<Team> ResolveAsync(System.Data.Common.DbDataReader reader, System.Threading.CancellationToken token)
        {
            var id = TeamId.From(await reader.GetFieldValueAsync<string>(0, token));
            if (_identityMap.TryGetValue(id, out var existing)) return existing;

            Team document;
            document = await _serializer.FromJsonAsync<Team>(reader, 1, token).ConfigureAwait(false);
            _session.MarkAsDocumentLoaded(id, document);
            _identityMap[id] = document;
            StoreTracker(_session, document);
            return document;
        }

    }

    // END: DirtyTrackingTeamSelector389145516


    // START: QueryOnlyTeamDocumentStorage389145516
    public class QueryOnlyTeamDocumentStorage389145516 : Marten.Internal.Storage.QueryOnlyDocumentStorage<Team, TeamId>
    {
        private readonly Marten.Schema.DocumentMapping _document;

        public QueryOnlyTeamDocumentStorage389145516(Marten.Schema.DocumentMapping document) : base(document)
        {
            _document = document;
        }



        public override TeamId AssignIdentity(Team document, string tenantId, Marten.Storage.IMartenDatabase database)
        {
            return document.Id.Value;
            return document.Id.Value;
        }


        public override Marten.Internal.Operations.IStorageOperation Update(Team document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.UpdateTeamOperation389145516
            (
                document, Identity(document),
                session.Versions.ForType<Team, TeamId>(),
                _document

            );
        }


        public override Marten.Internal.Operations.IStorageOperation Insert(Team document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.InsertTeamOperation389145516
            (
                document, Identity(document),
                session.Versions.ForType<Team, TeamId>(),
                _document

            );
        }


        public override Marten.Internal.Operations.IStorageOperation Upsert(Team document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.UpsertTeamOperation389145516
            (
                document, Identity(document),
                session.Versions.ForType<Team, TeamId>(),
                _document

            );
        }


        public override Marten.Internal.Operations.IStorageOperation Overwrite(Team document, Marten.Internal.IMartenSession session, string tenant)
        {
            throw new System.NotSupportedException();
        }


        public override TeamId Identity(Team document)
        {
            return document.Id.Value;
        }


        public override Marten.Linq.Selectors.ISelector BuildSelector(Marten.Internal.IMartenSession session)
        {
            return new Marten.Generated.DocumentStorage.QueryOnlyTeamSelector389145516(session, _document);
        }


        public override object RawIdentityValue(TeamId id)
        {
            return id.Value;
        }


        public override Npgsql.NpgsqlParameter BuildManyIdParameter(TeamId[] ids)
        {
            return new(){Value = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(ids, x => x.Value)), NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Text};
        }

    }

    // END: QueryOnlyTeamDocumentStorage389145516


    // START: LightweightTeamDocumentStorage389145516
    public class LightweightTeamDocumentStorage389145516 : Marten.Internal.Storage.LightweightDocumentStorage<Team, TeamId>
    {
        private readonly Marten.Schema.DocumentMapping _document;

        public LightweightTeamDocumentStorage389145516(Marten.Schema.DocumentMapping document) : base(document)
        {
            _document = document;
        }



        public override TeamId AssignIdentity(Team document, string tenantId, Marten.Storage.IMartenDatabase database)
        {
            return document.Id.Value;
            return document.Id.Value;
        }


        public override Marten.Internal.Operations.IStorageOperation Update(Team document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.UpdateTeamOperation389145516
            (
                document, Identity(document),
                session.Versions.ForType<Team, TeamId>(),
                _document

            );
        }


        public override Marten.Internal.Operations.IStorageOperation Insert(Team document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.InsertTeamOperation389145516
            (
                document, Identity(document),
                session.Versions.ForType<Team, TeamId>(),
                _document

            );
        }


        public override Marten.Internal.Operations.IStorageOperation Upsert(Team document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.UpsertTeamOperation389145516
            (
                document, Identity(document),
                session.Versions.ForType<Team, TeamId>(),
                _document

            );
        }


        public override Marten.Internal.Operations.IStorageOperation Overwrite(Team document, Marten.Internal.IMartenSession session, string tenant)
        {
            throw new System.NotSupportedException();
        }


        public override TeamId Identity(Team document)
        {
            return document.Id.Value;
        }


        public override Marten.Linq.Selectors.ISelector BuildSelector(Marten.Internal.IMartenSession session)
        {
            return new Marten.Generated.DocumentStorage.LightweightTeamSelector389145516(session, _document);
        }


        public override object RawIdentityValue(TeamId id)
        {
            return id.Value;
        }


        public override Npgsql.NpgsqlParameter BuildManyIdParameter(TeamId[] ids)
        {
            return new(){Value = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(ids, x => x.Value)), NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Text};
        }

    }

    // END: LightweightTeamDocumentStorage389145516


    // START: IdentityMapTeamDocumentStorage389145516
    public class IdentityMapTeamDocumentStorage389145516 : Marten.Internal.Storage.IdentityMapDocumentStorage<Team, TeamId>
    {
        private readonly Marten.Schema.DocumentMapping _document;

        public IdentityMapTeamDocumentStorage389145516(Marten.Schema.DocumentMapping document) : base(document)
        {
            _document = document;
        }



        public override TeamId AssignIdentity(Team document, string tenantId, Marten.Storage.IMartenDatabase database)
        {
            return document.Id.Value;
            return document.Id.Value;
        }


        public override Marten.Internal.Operations.IStorageOperation Update(Team document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.UpdateTeamOperation389145516
            (
                document, Identity(document),
                session.Versions.ForType<Team, TeamId>(),
                _document

            );
        }


        public override Marten.Internal.Operations.IStorageOperation Insert(Team document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.InsertTeamOperation389145516
            (
                document, Identity(document),
                session.Versions.ForType<Team, TeamId>(),
                _document

            );
        }


        public override Marten.Internal.Operations.IStorageOperation Upsert(Team document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.UpsertTeamOperation389145516
            (
                document, Identity(document),
                session.Versions.ForType<Team, TeamId>(),
                _document

            );
        }


        public override Marten.Internal.Operations.IStorageOperation Overwrite(Team document, Marten.Internal.IMartenSession session, string tenant)
        {
            throw new System.NotSupportedException();
        }


        public override TeamId Identity(Team document)
        {
            return document.Id.Value;
        }


        public override Marten.Linq.Selectors.ISelector BuildSelector(Marten.Internal.IMartenSession session)
        {
            return new Marten.Generated.DocumentStorage.IdentityMapTeamSelector389145516(session, _document);
        }


        public override object RawIdentityValue(TeamId id)
        {
            return id.Value;
        }


        public override Npgsql.NpgsqlParameter BuildManyIdParameter(TeamId[] ids)
        {
            return new(){Value = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(ids, x => x.Value)), NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Text};
        }

    }

    // END: IdentityMapTeamDocumentStorage389145516


    // START: DirtyTrackingTeamDocumentStorage389145516
    public class DirtyTrackingTeamDocumentStorage389145516 : Marten.Internal.Storage.DirtyCheckedDocumentStorage<Team, TeamId>
    {
        private readonly Marten.Schema.DocumentMapping _document;

        public DirtyTrackingTeamDocumentStorage389145516(Marten.Schema.DocumentMapping document) : base(document)
        {
            _document = document;
        }



        public override TeamId AssignIdentity(Team document, string tenantId, Marten.Storage.IMartenDatabase database)
        {
            return document.Id.Value;
            return document.Id.Value;
        }


        public override Marten.Internal.Operations.IStorageOperation Update(Team document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.UpdateTeamOperation389145516
            (
                document, Identity(document),
                session.Versions.ForType<Team, TeamId>(),
                _document

            );
        }


        public override Marten.Internal.Operations.IStorageOperation Insert(Team document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.InsertTeamOperation389145516
            (
                document, Identity(document),
                session.Versions.ForType<Team, TeamId>(),
                _document

            );
        }


        public override Marten.Internal.Operations.IStorageOperation Upsert(Team document, Marten.Internal.IMartenSession session, string tenant)
        {

            return new Marten.Generated.DocumentStorage.UpsertTeamOperation389145516
            (
                document, Identity(document),
                session.Versions.ForType<Team, TeamId>(),
                _document

            );
        }


        public override Marten.Internal.Operations.IStorageOperation Overwrite(Team document, Marten.Internal.IMartenSession session, string tenant)
        {
            throw new System.NotSupportedException();
        }


        public override TeamId Identity(Team document)
        {
            return document.Id.Value;
        }


        public override Marten.Linq.Selectors.ISelector BuildSelector(Marten.Internal.IMartenSession session)
        {
            return new Marten.Generated.DocumentStorage.DirtyTrackingTeamSelector389145516(session, _document);
        }


        public override object RawIdentityValue(TeamId id)
        {
            return id.Value;
        }


        public override Npgsql.NpgsqlParameter BuildManyIdParameter(TeamId[] ids)
        {
            return new(){Value = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(ids, x => x.Value)), NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Text};
        }

    }

    // END: DirtyTrackingTeamDocumentStorage389145516


    // START: TeamBulkLoader389145516
    public class TeamBulkLoader389145516 : Marten.Internal.CodeGeneration.BulkLoader<Team, TeamId>
    {
        private readonly Marten.Internal.Storage.IDocumentStorage<Team, TeamId> _storage;

        public TeamBulkLoader389145516(Marten.Internal.Storage.IDocumentStorage<Team, TeamId> storage) : base(storage)
        {
            _storage = storage;
        }


        public const string MAIN_LOADER_SQL = "COPY strong_typed8.mt_doc_team(\"mt_dotnet_type\", \"id\", \"mt_version\", \"data\") FROM STDIN BINARY";

        public const string TEMP_LOADER_SQL = "COPY mt_doc_team_temp(\"mt_dotnet_type\", \"id\", \"mt_version\", \"data\") FROM STDIN BINARY";

        public const string COPY_NEW_DOCUMENTS_SQL = "insert into strong_typed8.mt_doc_team (\"id\", \"data\", \"mt_version\", \"mt_dotnet_type\", mt_last_modified) (select mt_doc_team_temp.\"id\", mt_doc_team_temp.\"data\", mt_doc_team_temp.\"mt_version\", mt_doc_team_temp.\"mt_dotnet_type\", transaction_timestamp() from mt_doc_team_temp left join strong_typed8.mt_doc_team on mt_doc_team_temp.id = strong_typed8.mt_doc_team.id where strong_typed8.mt_doc_team.id is null)";

        public const string OVERWRITE_SQL = "update strong_typed8.mt_doc_team target SET data = source.data, mt_version = source.mt_version, mt_dotnet_type = source.mt_dotnet_type, mt_last_modified = transaction_timestamp() FROM mt_doc_team_temp source WHERE source.id = target.id";

        public const string CREATE_TEMP_TABLE_FOR_COPYING_SQL = "create temporary table mt_doc_team_temp (like strong_typed8.mt_doc_team including defaults)";


        public override string CreateTempTableForCopying()
        {
            return CREATE_TEMP_TABLE_FOR_COPYING_SQL;
        }


        public override string CopyNewDocumentsFromTempTable()
        {
            return COPY_NEW_DOCUMENTS_SQL;
        }


        public override string OverwriteDuplicatesFromTempTable()
        {
            return OVERWRITE_SQL;
        }


        public override void LoadRow(Npgsql.NpgsqlBinaryImporter writer, Team document, Marten.Storage.Tenant tenant, Marten.ISerializer serializer)
        {
            writer.Write(document.GetType().FullName, NpgsqlTypes.NpgsqlDbType.Varchar);
            writer.Write(document.Id.Value.Value, NpgsqlTypes.NpgsqlDbType.Text);
            writer.Write(Marten.Schema.Identity.CombGuidIdGeneration.NewGuid(), NpgsqlTypes.NpgsqlDbType.Uuid);
            writer.Write(serializer.ToJson(document), NpgsqlTypes.NpgsqlDbType.Jsonb);
        }


        public override async System.Threading.Tasks.Task LoadRowAsync(Npgsql.NpgsqlBinaryImporter writer, Team document, Marten.Storage.Tenant tenant, Marten.ISerializer serializer, System.Threading.CancellationToken cancellation)
        {
            await writer.WriteAsync(document.GetType().FullName, NpgsqlTypes.NpgsqlDbType.Varchar, cancellation);
            await writer.WriteAsync(document.Id.Value.Value, NpgsqlTypes.NpgsqlDbType.Text, cancellation);
            await writer.WriteAsync(Marten.Schema.Identity.CombGuidIdGeneration.NewGuid(), NpgsqlTypes.NpgsqlDbType.Uuid, cancellation);
            await writer.WriteAsync(serializer.ToJson(document), NpgsqlTypes.NpgsqlDbType.Jsonb, cancellation);
        }


        public override string MainLoaderSql()
        {
            return MAIN_LOADER_SQL;
        }


        public override string TempLoaderSql()
        {
            return TEMP_LOADER_SQL;
        }

    }

    // END: TeamBulkLoader389145516


    // START: TeamProvider389145516
    public class TeamProvider389145516 : Marten.Internal.Storage.DocumentProvider<Team>
    {
        private readonly Marten.Schema.DocumentMapping _mapping;

        public TeamProvider389145516(Marten.Schema.DocumentMapping mapping) : base(new TeamBulkLoader389145516(new QueryOnlyTeamDocumentStorage389145516(mapping)), new QueryOnlyTeamDocumentStorage389145516(mapping), new LightweightTeamDocumentStorage389145516(mapping), new IdentityMapTeamDocumentStorage389145516(mapping), new DirtyTrackingTeamDocumentStorage389145516(mapping))
        {
            _mapping = mapping;
        }


    }

    // END: TeamProvider389145516


}

