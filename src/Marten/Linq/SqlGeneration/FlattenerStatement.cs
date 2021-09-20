using System;
using Marten.Internal;
using Marten.Linq.Fields;
using Weasel.Postgresql;

namespace Marten.Linq.SqlGeneration
{
    internal class FlattenerStatement : Statement
    {
        private readonly ArrayField _field;
        private readonly string _sourceTable;

        public FlattenerStatement(ArrayField field, IMartenSession session, Statement sourceStatement) : base(null)
        {
            if (sourceStatement.FromObject.IsEmpty()) throw new ArgumentOutOfRangeException("The parent statement has an empty FromObject");

            _sourceTable = sourceStatement.FromObject;
            _field = field;

            ConvertToCommonTableExpression(session);
            sourceStatement.InsertBefore(this);
        }

        protected override void configure(CommandBuilder sql)
        {
            startCommonTableExpression(sql);

            sql.Append("select ctid, ");
            sql.Append(_field.LocatorForFlattenedElements);
            sql.Append(" as data from ");

            sql.Append(_sourceTable);


            if (Where != null)
            {
                sql.Append(" as d WHERE ");
                Where.Apply(sql);

                endCommonTableExpression(sql);
            }
            else
            {
                endCommonTableExpression(sql, " as d");
            }


        }
    }
}
