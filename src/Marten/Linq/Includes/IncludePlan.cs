using System;
using Marten.Internal;
using Marten.Internal.Storage;
using Marten.Linq.Fields;
using Marten.Linq.Selectors;
using Marten.Linq.SqlGeneration;
using Weasel.Postgresql;
using Marten.Util;

namespace Marten.Linq.Includes
{
    internal class IncludePlan<T> : IIncludePlan
    {
        private readonly IDocumentStorage<T> _storage;
        private readonly Action<T> _callback;

        public IncludePlan(IDocumentStorage<T> storage, IField connectingField, Action<T> callback)
        {
            _storage = storage;
            ConnectingField = connectingField;
            _callback = callback;
        }

        public Type DocumentType => typeof(T);

        public IField ConnectingField { get; }

        public int Index
        {
            set
            {
                IdAlias = "id" + (value + 1);
                ExpressionName = "include"+ (value + 1);

                TempTableSelector = RequiresLateralJoin()
                    ? $"{ExpressionName}.{IdAlias}"
                    : $"{ConnectingField.LocatorForIncludedDocumentId} as {IdAlias}";
            }
        }

        public bool RequiresLateralJoin()
        {
            return ConnectingField is ArrayField;
        }

        public string LeftJoinExpression => $"LEFT JOIN LATERAL {ConnectingField.LocatorForIncludedDocumentId} WITH ORDINALITY as {ExpressionName}({IdAlias}) ON TRUE";

        public string ExpressionName { get; private set; }

        public string IdAlias { get; private set; }
        public string TempTableSelector { get; private set; }

        public Statement BuildStatement(string tempTableName, IPagedStatement paging)
        {
            return new IncludedDocumentStatement(_storage, this, tempTableName, paging);
        }

        public IIncludeReader BuildReader(IMartenSession session)
        {
            var selector = (ISelector<T>) _storage.BuildSelector(session);
            return new IncludeReader<T>(_callback, selector);
        }

        public class IncludedDocumentStatement : SelectorStatement
        {
            public IncludedDocumentStatement(IDocumentStorage<T> storage, IncludePlan<T> includePlan,
                string tempTableName, IPagedStatement paging) : base(storage, storage.Fields)
            {
                var initial = new InTempTableWhereFragment(tempTableName, includePlan.IdAlias, paging);
                Where = storage.FilterDocuments(null, initial);
            }

            protected override void configure(CommandBuilder sql)
            {
                base.configure(sql);
                sql.Append(";\n");
            }
        }
    }


}
