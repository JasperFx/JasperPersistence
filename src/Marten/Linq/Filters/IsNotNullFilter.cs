using Marten.Linq.Fields;
using Weasel.Postgresql;
using Weasel.Postgresql.SqlGeneration;

namespace Marten.Linq.Filters;

public class IsNotNullFilter: IReversibleWhereFragment
{
    public IsNotNullFilter(IField field)
    {
        Field = field;
    }

    public IField Field { get; }

    public void Apply(CommandBuilder builder)
    {
        builder.Append(Field.RawLocator);
        builder.Append(" is not null");
    }

    public bool Contains(string sqlText)
    {
        return Field.Contains(sqlText);
    }

    public ISqlFragment Reverse()
    {
        return new IsNullFilter(Field);
    }
}
