﻿using System.Collections.Generic;
using System.Linq;
using Baseline;
using System.Text.RegularExpressions;

namespace Marten.Schema
{
    public class IndexDefinition : IIndexDefinition
    {
        private readonly DocumentMapping _parent;
        private readonly string[] _columns;
        private string _indexName;

        public IndexDefinition(DocumentMapping parent, params string[] columns)
        {
            _parent = parent;
            _columns = columns;
        }

        public IndexMethod Method { get; set; } = IndexMethod.btree;

        public bool IsUnique { get; set; }

        public bool IsConcurrent { get; set; }

        public string IndexName
        {
            get
            {
                if (_indexName.IsNotEmpty())
                {
                    return _indexName.StartsWith(DocumentMapping.MartenPrefix)
                        ? _indexName
                        : DocumentMapping.MartenPrefix + _indexName;
                }
                return $"{_parent.Table.Name}_idx_{_columns.Join("_")}";
            }
            set { _indexName = value; }
        }

        public string Expression { get; set; }

        public string Modifier { get; set; }

        public IEnumerable<string> Columns => _columns;

        public string ToDDL()
        {
            var index = IsUnique ? "CREATE UNIQUE INDEX" : "CREATE INDEX";
            if (IsConcurrent)
            {
                index += " CONCURRENTLY";
            }

            index += $" {IndexName} ON {_parent.Table.QualifiedName}";

            if (Method != IndexMethod.btree)
            {
                index += $" USING {Method}";
            }

            var columns = _columns.Select(column => $"\"{column}\"").Join(", ");
            if (Expression.IsEmpty())
            {
                index += $" ({columns})";
            }
            else
            {
                index += $" ({Expression.Replace("?", columns)})";
            }

            if (Modifier.IsNotEmpty())
            {
                index += " " + Modifier;
            }

            return index + ";";
        }

        public bool Matches(ActualIndex index)
        {
            if (!index.Name.EqualsIgnoreCase(IndexName)) return false;

            var actual = index.DDL;
            if (Method == IndexMethod.btree)
            {
                actual = actual.Replace("USING btree", "");
            }

            _columns.Each(col =>
            {
                actual = Regex.Replace(actual, "\\((?<column>[\\w_]+) (?<operatorclass>[\\w_]+)\\)", "(\"${column}\" ${operatorclass})");
            });

            if (!actual.Contains(_parent.Table.QualifiedName))
            {
                actual = actual.Replace("ON " + _parent.Table.Name, "ON " + _parent.Table.QualifiedName);
            }

            actual = actual.Replace("  ", " ") + ";";

            return ToDDL().EqualsIgnoreCase(actual);
        }
    }

    public enum IndexMethod
    {
        btree,
        hash,
        gist,
        gin,
        brin
    }
}