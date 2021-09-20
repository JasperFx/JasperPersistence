using System;
using System.Linq.Expressions;
using Marten.Internal;
using Marten.Linq.Fields;
using Weasel.Postgresql;
using Marten.Util;
using Weasel.Postgresql.SqlGeneration;

namespace Marten.Linq.SqlGeneration
{
    /// <summary>
    ///     Used when doing a Where(x => x.Children.Count(c => ....) > #) kind of filter
    /// </summary>
    internal class CountComparisonStatement: JsonStatement, IComparableFragment
    {
        private readonly string _tableName;
        private readonly FlattenerStatement _flattened;

        public CountComparisonStatement(IMartenSession session, Type documentType, IFieldMapping fields,
            FlattenerStatement parent): base(documentType, fields, parent)
        {
            ConvertToCommonTableExpression(session);
            parent.InsertAfter(this);

            _tableName = parent.ExportName;

            _flattened = parent;
        }

        protected override bool IsSubQuery => true;

        public string Operator { get; private set; } = "=";

        public CommandParameter Value { get; private set; }

        public ISqlFragment CreateComparison(string op, ConstantExpression value, Expression memberExpression)
        {
            Value = new CommandParameter(value);
            Operator = op;
            return new WhereCtIdInSubQuery(ExportName, _flattened);
        }

        protected override void configure(CommandBuilder sql)
        {
            startCommonTableExpression(sql);


            sql.Append("select ctid, count(*) as data from ");
            sql.Append(_tableName);
            sql.Append(" as d");
            writeWhereClause(sql);
            sql.Append(" group by ctid having count(*) ");
            sql.Append(Operator);

            Value.Apply(sql);

            endCommonTableExpression(sql);
        }
    }
}
