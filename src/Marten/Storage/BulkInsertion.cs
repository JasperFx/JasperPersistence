﻿using System;
using System.Collections.Generic;
using System.Linq;
using Baseline;
using Marten.Services;
using Marten.Util;
using Npgsql;

namespace Marten.Storage
{
    public class BulkInsertion : IDisposable
    {
        private readonly ITenant _tenant;
        private readonly CharArrayTextWriter.IPool _writerPool;

        public BulkInsertion(ITenant tenant, StoreOptions options, CharArrayTextWriter.IPool writerPool)
        {
            _tenant = tenant;
            _writerPool = writerPool;
            Serializer = options.Serializer();
        }

        public ISerializer Serializer { get;}

        public void BulkInsert<T>(T[] documents, BulkInsertMode mode = BulkInsertMode.InsertsOnly, int batchSize = 1000)
        {
            if (typeof(T) == typeof(object))
            {
                BulkInsertDocuments(documents.OfType<object>(), mode: mode);
            }
            else
            {
                using (var conn = _tenant.CreateConnection())
                {
                    conn.Open();
                    var tx = conn.BeginTransaction();

                    try
                    {
                        bulkInsertDocuments(documents, batchSize, conn, mode);

                        tx.Commit();
                    }
                    catch (Exception)
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public void BulkInsertDocuments(IEnumerable<object> documents, BulkInsertMode mode = BulkInsertMode.InsertsOnly, int batchSize = 1000)
        {
            var groups =
                documents.Where(x => x != null)
                    .GroupBy(x => x.GetType())
                    .Select(group => typeof(BulkInserter<>).CloseAndBuildAs<IBulkInserter>(@group, @group.Key))
                    .ToArray();

            using (var conn = _tenant.CreateConnection())
            {
                conn.Open();
                var tx = conn.BeginTransaction();

                try
                {
                    foreach (var @group in groups)
                    {
                        @group.BulkInsert(batchSize, conn, this, mode);
                    }

                    tx.Commit();
                }
                catch (Exception)
                {
                    tx.Rollback();
                    throw;
                }
            }
        }


        internal interface IBulkInserter
        {
            void BulkInsert(int batchSize, NpgsqlConnection connection, BulkInsertion parent, BulkInsertMode mode);
        }

        internal class BulkInserter<T> : IBulkInserter
        {
            private readonly T[] _documents;

            public BulkInserter(IEnumerable<object> documents)
            {
                _documents = documents.OfType<T>().ToArray();
            }

            public void BulkInsert(int batchSize, NpgsqlConnection connection, BulkInsertion parent, BulkInsertMode mode)
            {
                parent.bulkInsertDocuments(_documents, batchSize, connection, mode);
            }
        }

        private void bulkInsertDocuments<T>(T[] documents, int batchSize, NpgsqlConnection conn, BulkInsertMode mode)
        {
            var loader = _tenant.BulkLoaderFor<T>();

            if (mode != BulkInsertMode.InsertsOnly)
            {
                var sql = loader.CreateTempTableForCopying();
                conn.RunSql(sql);
            }

            var writer = _writerPool.Lease();
            try
            {
                if (documents.Length <= batchSize)
                {
                    if (mode == BulkInsertMode.InsertsOnly)
                    {
                        loader.Load(_tenant, Serializer, conn, documents, writer);
                    }
                    else
                    {
                        loader.LoadIntoTempTable(_tenant, Serializer, conn, documents, writer);
                    }

                }
                else
                {
                    var total = 0;
                    var page = 0;

                    while (total < documents.Length)
                    {
                        var batch = documents.Skip(page * batchSize).Take(batchSize).ToArray();

                        if (mode == BulkInsertMode.InsertsOnly)
                        {
                            loader.Load(_tenant, Serializer, conn, batch, writer);
                        }
                        else
                        {
                            loader.LoadIntoTempTable(_tenant, Serializer, conn, batch, writer);
                        }


                        page++;
                        total += batch.Length;
                    }
                }
            }
            finally
            {
                if (writer != null)
                {
                    _writerPool.Release(writer);
                }
            }

            if (mode == BulkInsertMode.IgnoreDuplicates)
            {
                var copy = loader.CopyNewDocumentsFromTempTable();

                conn.RunSql(copy);
            }
            else if (mode == BulkInsertMode.OverwriteExisting)
            {
                var overwrite = loader.OverwriteDuplicatesFromTempTable();
                var copy = loader.CopyNewDocumentsFromTempTable();

                conn.RunSql(overwrite, copy);
            }
        }

        public void Dispose()
        {
            _writerPool?.Dispose();
        }
    }
}