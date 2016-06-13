﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Baseline;
using Marten.Schema;
using Marten.Services;
using Marten.Util;

namespace Marten.Transforms
{
    public class TransformFunction : ISchemaObjects
    {
        public static readonly string Prefix = "mt_transform_";

        private readonly StoreOptions _options;
        private bool _checked;

        public TransformFunction(StoreOptions options, string name, string body)
        {
            _options = options;
            Name = name;
            Body = body;

            Function = new FunctionName(_options.DatabaseSchemaName, $"{Prefix}{Name.ToLower().Replace(".", "_")}");
        }

        public string Name { get; set; }
        public string Body { get; set; }

        public FunctionName Function { get; }

        public readonly IList<string> OtherArgs = new List<string>();

        private IEnumerable<string> allArgs()
        {
            return new [] {"doc"}.Concat(OtherArgs);
        }

        public void GenerateSchemaObjectsIfNecessary(AutoCreate autoCreateSchemaObjectsMode, IDocumentSchema schema,
            IDDLRunner runner)
        {
            if (_checked) return;


            var shouldReload = functionShouldBeReloaded(schema);
            if (!shouldReload)
            {
                _checked = true;
                return;
            }


            if (autoCreateSchemaObjectsMode == AutoCreate.None)
            {
                string message =
                    $"The transform function {Function.QualifiedName} and cannot be created dynamically unless the {nameof(StoreOptions)}.{nameof(StoreOptions.AutoCreateSchemaObjects)} is higher than \"None\". See http://jasperfx.github.io/marten/documentation/documents/ for more information";
                throw new InvalidOperationException(message);
            }

            runner.Apply(this, GenerateFunction());
        }

        public void WriteSchemaObjects(IDocumentSchema schema, StringWriter writer)
        {
            writer.WriteLine(GenerateFunction());
        }

        public void RemoveSchemaObjects(IManagedConnection connection)
        {
            var signature = allArgs().Select(x => "JSONB").Join(", ");
            var dropSql = $"DROP FUNCTION IF EXISTS {Function.QualifiedName}({signature})";
            connection.Execute(cmd => cmd.Sql(dropSql).ExecuteNonQuery());
        }

        public void ResetSchemaExistenceChecks()
        {
            _checked = false;
        }

        public void WritePatch(IDocumentSchema schema, IDDLRunner runner)
        {
            if (functionShouldBeReloaded(schema))
            {
                runner.Apply(this, GenerateFunction());
            }
        }

        public string GenerateFunction()
        {
            var defaultExport = "{export: {}}";

            var signature = allArgs().Select(x => $"{x} JSONB").Join(", ");
            var args = allArgs().Join(", ");

            return
                $@"
CREATE OR REPLACE FUNCTION {Function.QualifiedName}({signature}) RETURNS JSONB AS $$

  var module = {defaultExport};

  {Body}

  var func = module.exports;

  return func({args});

$$ LANGUAGE plv8 IMMUTABLE STRICT;
";
        }


        public static TransformFunction ForFile(StoreOptions options, string file, string name = null)
        {
            var body = new FileSystem().ReadStringFromFile(file);
            name = name ?? Path.GetFileNameWithoutExtension(file).ToLowerInvariant();

            return new TransformFunction(options, name, body);
        }

        private bool functionShouldBeReloaded(IDocumentSchema schema)
        {
            var definition = schema.DbObjects.DefinitionForFunction(Function);
            return definition.IsEmpty() || !definition.Contains(Body);
        }

        public override string ToString()
        {
            return $"Transform Function '{Name}'";
        }
    }
}