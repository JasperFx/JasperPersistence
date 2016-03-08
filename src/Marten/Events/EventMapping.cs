using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Baseline;
using Marten.Linq;
using Marten.Schema;
using Marten.Services;
using Marten.Util;
using Npgsql;
using NpgsqlTypes;

namespace Marten.Events
{
    public abstract class EventMapping : IDocumentMapping
    {
        private readonly EventGraph _parent;
        protected readonly DocumentMapping _inner;
        // TODO -- this logic is duplicated. Centralize in an ext method
        public static string ToEventTypeName(Type eventType)
        {
            return eventType.Name.SplitPascalCase().ToLower().Replace(" ", "_");
        }

        protected EventMapping(EventGraph parent, Type eventType)
        {
            _parent = parent;
            DocumentType = eventType;

            EventTypeName = Alias = ToEventTypeName(DocumentType);
            IdMember = DocumentType.GetProperty(nameof(IEvent.Id));

            // TODO -- may need to pull StoreOptions through here.
            _inner = new DocumentMapping(eventType);
        }

        public Type DocumentType { get; }
        public string EventTypeName { get; set; }
        public string Alias { get; }
        public MemberInfo IdMember { get; }
        public IIdGeneration IdStrategy { get; } = new GuidIdGeneration();
        public NpgsqlDbType IdType { get; } = NpgsqlDbType.Uuid;
        public string TableName { get; } = "mt_events";
        public PropertySearching PropertySearching { get; } = PropertySearching.JSON_Locator_Only;

        public string SelectFields(string tableAlias)
        {
            return $"{tableAlias}.id, {tableAlias}.data";
        }

        public void GenerateSchemaObjectsIfNecessary(AutoCreate autoCreateSchemaObjectsMode, IDocumentSchema schema, Action<string> executeSql)
        {
            _parent.GenerateSchemaObjectsIfNecessary(autoCreateSchemaObjectsMode, schema, executeSql);
        }

        public IField FieldFor(IEnumerable<MemberInfo> members)
        {
            return _inner.FieldFor(members);
        }

        public IWhereFragment FilterDocuments(IWhereFragment query)
        {
            return new CompoundWhereFragment("and", DefaultWhereFragment(), query);
        }

        public IWhereFragment DefaultWhereFragment()
        {
            return new WhereFragment($"d.type = '{EventTypeName}'");
        }

        public abstract IDocumentStorage BuildStorage(IDocumentSchema schema);

        public void WriteSchemaObjects(IDocumentSchema schema, StringWriter writer)
        {
            _parent.WriteSchemaObjects(schema, writer);
        }

        public void RemoveSchemaObjects(IManagedConnection connection)
        {
            throw new NotSupportedException($"Invalid to remove schema objects for {DocumentType}");
        }

        public void DeleteAllDocuments(IConnectionFactory factory)
        {
            factory.RunSql($"delete from mt_events where type = '{Alias}'");
        }
    }

    public class EventMapping<T> : EventMapping, IDocumentStorage, IResolver<T> where T : class, IEvent
    {
        public EventMapping(EventGraph parent) : base(parent, typeof(T))
        {

        }

        public override IDocumentStorage BuildStorage(IDocumentSchema schema)
        {
            return this;
        }

        public NpgsqlCommand LoaderCommand(object id)
        {
            return new NpgsqlCommand($"select d.data, d.id from mt_events as d where id = :id and type = '{Alias}'").With("id", id);
        }

        public NpgsqlCommand DeleteCommandForId(object id)
        {
            throw new NotSupportedException();
        }

        public NpgsqlCommand DeleteCommandForEntity(object entity)
        {
            throw new NotSupportedException();
        }

        public NpgsqlCommand LoadByArrayCommand<TKey>(TKey[] ids)
        {
            return new NpgsqlCommand($"select d.data, d.id from mt_events as d where id = ANY(:ids) and type = '{Alias}'").With("ids", ids);
        }

        public object Identity(object document)
        {
            return document.As<IEvent>().Id;
        }

        public void RegisterUpdate(UpdateBatch batch, object entity)
        {
            // Do nothing
        }

        public void RegisterUpdate(UpdateBatch batch, object entity, string json)
        {
            // Do nothing
        }

        public void Remove(IIdentityMap map, object entity)
        {
            throw new InvalidOperationException("Use IDocumentSession.Events for all persistence of IEvent objects");
        }

        public void Delete(IIdentityMap map, object id)
        {
            throw new InvalidOperationException("Use IDocumentSession.Events for all persistence of IEvent objects");
        }

        public void Store(IIdentityMap map, object id, object entity)
        {
            throw new InvalidOperationException("Use IDocumentSession.Events for all persistence of IEvent objects");
        }

        public T Resolve(DbDataReader reader, IIdentityMap map)
        {
            var id = reader.GetGuid(0);
            var json = reader.GetString(1);

            return map.Get<T>(id, json);
        }

        public T Build(DbDataReader reader, ISerializer serializer)
        {
            return serializer.FromJson<T>(reader.GetString(0));
        }

        public T Resolve(IIdentityMap map, ILoader loader, object id)
        {
            return map.Get(id, () => loader.LoadDocument<T>(id));
        }

        public Task<T> ResolveAsync(IIdentityMap map, ILoader loader, CancellationToken token, object id)
        {
            return map.GetAsync(id, tkn => loader.LoadDocumentAsync<T>(id, tkn), token);
        }
    }


}