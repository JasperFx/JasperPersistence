using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Baseline;
using Marten.Linq;
using Marten.Schema.Hierarchies;
using Marten.Services;
using Marten.Services.Includes;
using Marten.Util;

namespace Marten.Schema
{
    public class SubClassMapping : IDocumentMapping
    {
        private readonly DocumentMapping _parent;
        private readonly DocumentMapping _inner;

        public SubClassMapping(Type documentType, DocumentMapping parent, StoreOptions storeOptions, string alias = null)
        {
            DocumentType = documentType;
            _inner = new DocumentMapping(documentType, storeOptions);
            _parent = parent;
            Alias = alias ?? documentType.GetTypeName().Replace(".", "_").SplitCamelCase().Replace(" ", "_").ToLowerInvariant();
        }


        public DocumentMapping Parent => _parent;


        public string Alias { get; set; }

        public string UpsertName => _parent.QualifiedUpsertName;
        public Type DocumentType { get; }

        public string QualifiedTableName => _parent.QualifiedTableName;
        public string TableName => _parent.TableName;

        public string DatabaseSchemaName
        {
            get { return _parent.DatabaseSchemaName; }
            set { throw new NotSupportedException("The DatabaseSchemaName of a sub class mapping can't be set. The DatabaseSchemaName of the parent will be used."); }
        }

        public PropertySearching PropertySearching => _parent.PropertySearching;

        public IIdGeneration IdStrategy
        {
            get { return _parent.IdStrategy; }
            set { throw new NotSupportedException("The IdStrategy of a sub class mapping can't be set. The IdStrategy of the parent will be used."); }
        }

        public IEnumerable<DuplicatedField> DuplicatedFields => _parent.DuplicatedFields;
        public MemberInfo IdMember => _parent.IdMember;
        public string[] SelectFields()
        {
            return _inner.SelectFields();
        }

        public void GenerateSchemaObjectsIfNecessary(AutoCreate autoCreateSchemaObjectsMode, IDocumentSchema schema, Action<string> executeSql)
        {
            _parent.GenerateSchemaObjectsIfNecessary(autoCreateSchemaObjectsMode, schema, executeSql);
        }

        public IField FieldFor(IEnumerable<MemberInfo> members)
        {
            return _parent.FieldFor(members) ?? _inner.FieldFor(members);
        }

        public IWhereFragment FilterDocuments(IWhereFragment query)
        {
            return new CompoundWhereFragment("and", DefaultWhereFragment(), query);
        }

        public IWhereFragment DefaultWhereFragment()
        {
            return new WhereFragment($"d.{DocumentMapping.DocumentTypeColumn} = '{Alias}'");
        }

        public IDocumentStorage BuildStorage(IDocumentSchema schema)
        {
            var parentStorage = _parent.BuildStorage(schema);
            return typeof (SubClassDocumentStorage<,>).CloseAndBuildAs<IDocumentStorage>(parentStorage, DocumentType,
                _parent.DocumentType);
        }

        public void WriteSchemaObjects(IDocumentSchema schema, StringWriter writer)
        {
            _parent.WriteSchemaObjects(schema, writer);
        }

        public void RemoveSchemaObjects(IManagedConnection connection)
        {
            throw new NotSupportedException($"Invalid to remove schema objects for {DocumentType}, Use the parent {_parent.DocumentType} instead");
        }

        public void DeleteAllDocuments(IConnectionFactory factory)
        {
            factory.RunSql($"delete from {_parent.QualifiedTableName} where {DocumentMapping.DocumentTypeColumn} = '{Alias}'");
        }

        public IncludeJoin<TOther> JoinToInclude<TOther>(JoinType joinType, IDocumentMapping other, MemberInfo[] members, Action<TOther> callback) where TOther : class
        {
            return _parent.JoinToInclude<TOther>(joinType, other, members, callback);
        }
    }


}