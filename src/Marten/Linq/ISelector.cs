﻿using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Baseline;
using Marten.Schema;
using Marten.Services;

namespace Marten.Linq
{
    public interface ISelector<T>
    {
        T Resolve(DbDataReader reader, IIdentityMap map);

        Task<T> ResolveAsync(DbDataReader reader, IIdentityMap map, CancellationToken token);

        string[] SelectFields();

        string ToSelectClause(IQueryableDocument mapping);
    }

    public abstract class BasicSelector
    {
        private readonly string[] _selectFields;
        private readonly bool _distinct = false;

        protected BasicSelector(params string[] selectFields)
        {
            _selectFields = selectFields;
        }

        protected BasicSelector(bool distinct, params string[] selectFields)
        {
            _selectFields = selectFields;
            _distinct = distinct;
        }

        public string[] SelectFields() => _selectFields;

        public string ToSelectClause(IQueryableDocument mapping)
        {
            return $"select {(_distinct ? "distinct " : "")}{SelectFields().Join(", ")} from {mapping.Table.QualifiedName} as d";
        }
    }

    public static class SelectorExtensions
    {
        public static IList<T> Read<T>(this ISelector<T> selector, DbDataReader reader, IIdentityMap map)
        {
            var list = new List<T>();

            while (reader.Read())
            {
                list.Add(selector.Resolve(reader, map));
            }

            return list;
        }

        public static async Task<IList<T>> ReadAsync<T>(this ISelector<T> selector, DbDataReader reader, IIdentityMap map, CancellationToken token)
        {
            var list = new List<T>();

            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                list.Add(await selector.ResolveAsync(reader, map, token).ConfigureAwait(false));
            }

            return list;
        }
    }
}