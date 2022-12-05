using Marten.Schema.Arguments;
using Marten.Storage.Metadata;
using Weasel.Postgresql;
using Weasel.Postgresql.SqlGeneration;

namespace Marten.Linq.Filters;

public class CurrentTenantFilter: ISqlFragment
{
    public static readonly string Filter = $"d.{TenantIdColumn.Name} = :{TenantIdArgument.ArgName}";

    public static readonly CurrentTenantFilter Instance = new();

    public void Apply(CommandBuilder builder)
    {
        builder.Append(Filter);
        builder.AddNamedParameter(TenantIdArgument.ArgName, "");
    }

    public bool Contains(string sqlText)
    {
        return Filter.Contains(sqlText);
    }
}
