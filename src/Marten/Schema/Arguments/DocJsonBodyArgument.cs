using System.Threading;
using JasperFx.CodeGeneration;
using JasperFx.CodeGeneration.Frames;
using JasperFx.CodeGeneration.Model;
using Marten.Internal;
using NpgsqlTypes;

namespace Marten.Schema.Arguments;

internal class DocJsonBodyArgument: UpsertArgument
{
    public DocJsonBodyArgument()
    {
        Arg = "doc";
        PostgresType = "JSONB";
        DbType = NpgsqlDbType.Jsonb;
        Column = "data";
    }


    public override void GenerateBulkWriterCode(GeneratedType type, GeneratedMethod load, DocumentMapping mapping)
    {
        load.Frames.Code(
            "writer.Write(serializer.ToJson(document), {0});",
            NpgsqlDbType.Jsonb);
    }

    public override void GenerateBulkWriterCodeAsync(GeneratedType type, GeneratedMethod load, DocumentMapping mapping)
    {
        load.Frames.CodeAsync(
            "await writer.WriteAsync(serializer.ToJson(document), {0}, {1});",
            NpgsqlDbType.Jsonb, Use.Type<CancellationToken>());
    }

    public override void GenerateCodeToSetDbParameterValue(GeneratedMethod method, GeneratedType type, int i,
        Argument parameters,
        DocumentMapping mapping, StoreOptions options)
    {
        method.Frames.Code($"{parameters.Usage}[{i}].NpgsqlDbType = {{0}};", NpgsqlDbType.Jsonb);
        method.Frames.Code($"{parameters.Usage}[{i}].Value = {{0}}.Serializer.ToJson(_document);",
            Use.Type<IMartenSession>());
    }
}
