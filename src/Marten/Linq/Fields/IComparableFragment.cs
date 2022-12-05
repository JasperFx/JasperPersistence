using System.Linq.Expressions;
using Weasel.Postgresql.SqlGeneration;

namespace Marten.Linq.Fields;

public interface IComparableFragment
{
    ISqlFragment CreateComparison(string op, ConstantExpression value, Expression memberExpression);
}
