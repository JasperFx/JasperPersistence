﻿using System;
using System.IO;
using Marten.Services;
using Marten.Util;

namespace Marten.Schema.Identity.Sequences
{
    public class SequenceFactory : ISequences, ISchemaObjects
    {
        private readonly IDocumentSchema _schema;
        private readonly IConnectionFactory _factory;
        private readonly StoreOptions _options;
        private readonly IMartenLogger _logger;
        private bool _checked;

        private TableName Table => new TableName(_options.DatabaseSchemaName, "mt_hilo");

        public SequenceFactory(IDocumentSchema schema, IConnectionFactory factory, StoreOptions options, IMartenLogger logger)
        {
            _schema = schema;
            _factory = factory;
            _options = options;
            _logger = logger;
        }

        public ISequence Hilo(Type documentType, HiloSettings settings)
        {
            return new HiloSequence(_factory, _options, documentType.Name, settings);
        }

        public void GenerateSchemaObjectsIfNecessary(AutoCreate autoCreateSchemaObjectsMode, IDocumentSchema schema, IDDLRunner runner)
        {
            if (_checked) return;

            _checked = true;

            if (!schema.DbObjects.TableExists(Table))
            {
                if (_options.AutoCreateSchemaObjects == AutoCreate.None)
                {
                    throw new InvalidOperationException($"Hilo table is missing, but {nameof(StoreOptions.AutoCreateSchemaObjects)} is {_options.AutoCreateSchemaObjects}");
                }

                WritePatch(schema, runner);
            }
        }

        public override string ToString()
        {
            return "Hilo Sequence Factory";
        }

        public void WriteSchemaObjects(IDocumentSchema schema, StringWriter writer)
        {
            var sqlScript = SchemaBuilder.GetSqlScript(Table.Schema, "mt_hilo");
            writer.WriteLine(sqlScript);
            writer.WriteLine("");
            writer.WriteLine("");
        }

        public void RemoveSchemaObjects(IManagedConnection connection)
        {
            var sql = "drop table if exists " + Table.QualifiedName;
            connection.Execute(cmd => cmd.Sql(sql).ExecuteNonQuery());
        }

        public void ResetSchemaExistenceChecks()
        {
            _checked = false;
        }

        public void WritePatch(IDocumentSchema schema, IDDLRunner runner)
        {
            if (!schema.DbObjects.TableExists(Table))
            {
                var sqlScript = SchemaBuilder.GetSqlScript(Table.Schema, "mt_hilo");
                runner.Apply(this, sqlScript);
            }
        }

        public string Name { get; } = "mt_hilo";
    }
}