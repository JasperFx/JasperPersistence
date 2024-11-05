#nullable enable
using Marten.Internal.Operations;
using Marten.Internal.Storage;
using Marten.Schema;
using Weasel.Core.Operations;
using Weasel.Postgresql;

namespace Marten.Linq.SqlGeneration;

internal class UnSoftDelete: IOperationFragment
{
    private readonly string _sql;

    public UnSoftDelete(IDocumentStorage storage)
    {
        _sql =
            $"update {storage.TableName.QualifiedName} as d set {SchemaConstants.DeletedColumn} = False, {SchemaConstants.DeletedAtColumn} = NULL";
    }

    public void Apply(IPostgresqlCommandBuilder builder)
    {
        builder.Append(_sql);
    }

    public OperationRole Role()
    {
        return OperationRole.Deletion;
    }
}
