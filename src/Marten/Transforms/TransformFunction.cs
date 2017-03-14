﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            return new string[] {"doc"}.Concat(OtherArgs);
        }

        public void GenerateSchemaObjectsIfNecessary(AutoCreate autoCreateSchemaObjectsMode, IDocumentSchema schema, SchemaPatch patch)
        {
            if (_checked) return;


            var diff  = functionDiff(schema);
            if (!diff.HasChanged)
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

            diff.WritePatch(schema.StoreOptions, patch);
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

        public void WritePatch(IDocumentSchema schema, SchemaPatch patch)
        {
            var diff = functionDiff(schema);

            if (diff.AllNew || diff.HasChanged)
            {
                diff.WritePatch(schema.StoreOptions, patch);
            }
        }

        public string ToDropSignature()
        {
            var signature = allArgs().Select(x => $"JSONB").Join(", ");
            return $"DROP FUNCTION IF EXISTS {Function.QualifiedName}({signature});";
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

        private FunctionDiff functionDiff(IDocumentSchema schema)
        {
            var body = schema.DbObjects.DefinitionForFunction(Function);
            var expected = new FunctionBody(Function, new string[] {ToDropSignature()}, GenerateFunction());

            return new FunctionDiff(expected, body);
        }

        public override string ToString()
        {
            return $"Transform Function '{Name}'";
        }

        public void WritePatchForAllDocuments(SchemaPatch patch, string tableName, string fileName, bool includeImmediateInvocation = false)
        {
            patch.WriteTransactionalFile(fileName, GenerateTransformExecutionScript(tableName, includeImmediateInvocation));
        }

        public string GenerateTransformExecutionScript(string tableName, bool shouldImmediatelyInvoke = false)
        {
            var sqlBodyBuilder = new StringBuilder();
            sqlBodyBuilder.Append(GenerateFunction());

            var runExecutionOnDocumentData = $"var transformedDoc = plv8.execute('SELECT {Function.QualifiedName}($1)', doc.data);";
            var updateExistingDocumentData = $"plv8.execute('UPDATE {tableName} SET data = $1 WHERE id = $2', [transformedDoc[0][\"{Function.Name}\"], doc.id]);";

            var updateAllDocs = "docs.forEach(function(doc) {" + Environment.NewLine +
                                "    " + runExecutionOnDocumentData + Environment.NewLine +
                                "    " + updateExistingDocumentData + Environment.NewLine +
                                "  });";

            var executeTransformFunctionName = $"{_options.DatabaseSchemaName}.execute_transform_{Function.Name}";

            var functionInvocation = $@"
CREATE OR REPLACE FUNCTION {executeTransformFunctionName}()

RETURNS VOID as $$

  var docs = plv8.execute('select id, data from {tableName}');
  {updateAllDocs}

$$ LANGUAGE PLV8 IMMUTABLE STRICT;";

            sqlBodyBuilder.Append(functionInvocation);
            sqlBodyBuilder.AppendLine("");

            if (shouldImmediatelyInvoke)
            {
                sqlBodyBuilder.Append($"PERFORM {executeTransformFunctionName}();");
            }

            return sqlBodyBuilder.ToString();
        }
    }
}