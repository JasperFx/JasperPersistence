using System.Linq.Expressions;
using System.Reflection;
using NpgsqlTypes;
using Weasel.Postgresql.SqlGeneration;

namespace Marten.Linq.Fields;

public class EnumAsIntegerField: FieldBase
{
    public EnumAsIntegerField(string dataLocator, Casing casing, MemberInfo[] members): base(dataLocator, "integer",
        casing, members)
    {
        PgType = "integer";
        TypedLocator = $"CAST({RawLocator} as {PgType})";
    }

    public override string SelectorForDuplication(string pgType)
    {
        return $"CAST({RawLocator.Replace("d.", "")} as {PgType})";
    }

    public override ISqlFragment CreateComparison(string op, ConstantExpression value, Expression memberExpression)
    {
        var integer = (int)value.Value;
        return new ComparisonFilter(this, new CommandParameter(integer, NpgsqlDbType.Integer), op);
    }
}
