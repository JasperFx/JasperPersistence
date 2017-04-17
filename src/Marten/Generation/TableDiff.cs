using System;
using System.Linq;
using Baseline;
using Marten.Schema;
using Marten.Storage;

namespace Marten.Generation
{
    public class TableDiff
    {
        private readonly DbObjectName _tableName;

        public TableDiff(TableDefinition expected, TableDefinition actual)
        {
            Missing = expected.Columns.Where(x => actual.Columns.All(_ => _.Name != x.Name)).ToArray();
            Extras = actual.Columns.Where(x => expected.Columns.All(_ => _.Name != x.Name)).ToArray();
            Matched = expected.Columns.Intersect(actual.Columns).ToArray();
            Different =
                expected.Columns.Where(x => actual.HasColumn(x.Name) && !x.Equals(actual.Column(x.Name))).ToArray();

            _tableName = expected.Name;
        }

        public TableColumn[] Different { get; set; }

        public TableColumn[] Matched { get; set; }

        public TableColumn[] Extras { get; set; }

        public TableColumn[] Missing { get; set; }

        public bool Matches => Missing.Count() + Extras.Count() + Different.Count() == 0;

        public bool CanPatch()
        {
            return !Different.Any();
        }

        public void CreatePatch(DocumentMapping mapping, SchemaPatch runner)
        {
            var systemFields = new string[]
            {
                DocumentMapping.LastModifiedColumn,
                DocumentMapping.DotNetTypeColumn,
                DocumentMapping.VersionColumn,
                DocumentMapping.DeletedColumn,
                DocumentMapping.DeletedAtColumn,
                DocumentMapping.DocumentTypeColumn
            };

            var missingNonSystemFields = Missing.Where(x => !systemFields.Contains(x.Name)).ToArray();
            var fields = missingNonSystemFields.Select(x => mapping.FieldForColumn(x.Name)).ToArray();
            if (fields.Length != missingNonSystemFields.Length)
            {
                throw new InvalidOperationException("The expected columns did not match with the DocumentMapping");
            }


            var missingSystemColumns = Missing.Where(x => systemFields.Contains(x.Name)).ToArray();
            if (missingSystemColumns.Any())
            {
                missingSystemColumns.Each(col =>
                {
                    var patch =
                        $"alter table {_tableName.QualifiedName} add column {col.ToDeclaration(col.Name.Length + 1)};";

                    runner.Updates.Apply(this, patch);
                    runner.Rollbacks.RemoveColumn(this, mapping.Table, col.Name);
                });
            }


            fields.Each(x =>
            {
                x.WritePatch(mapping, runner);
                runner.Rollbacks.RemoveColumn(this, mapping.Table, x.ColumnName);
            });
        }

        public override string ToString()
        {
            return $"TableDiff for {_tableName}";
        }
    }
}