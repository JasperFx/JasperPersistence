﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Baseline;
using Marten.Linq;
using Marten.Schema;

namespace Marten.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDocumentSchema _schema;
        private readonly MartenExpressionParser _parser;
        private readonly ConcurrentDictionary<Type, IEnumerable> _updates = new ConcurrentDictionary<Type, IEnumerable>();
        private readonly ConcurrentDictionary<Type, IEnumerable> _inserts = new ConcurrentDictionary<Type, IEnumerable>();
        private readonly ConcurrentDictionary<Type, IList<Delete>> _deletes = new ConcurrentDictionary<Type, IList<Delete>>(); 
        private readonly IList<IDocumentTracker> _trackers = new List<IDocumentTracker>(); 

        public UnitOfWork(IDocumentSchema schema, MartenExpressionParser parser)
        {
            _schema = schema;
            _parser = parser;
        }

        public void AddTracker(IDocumentTracker tracker)
        {
            _trackers.Fill(tracker);
        }

        public void RemoveTracker(IDocumentTracker tracker)
        {
            _trackers.Remove(tracker);
        }

        private void delete<T>(object id)
        {
            var list = _deletes.GetOrAdd(typeof (T), _ => new List<Delete>());
            list.Add(new Delete(typeof(T), id));
        }

        public void DeleteEntity<T>(T entity)
        {
            var id = _schema.StorageFor(typeof(T)).Identity(entity);
            var list = _deletes.GetOrAdd(typeof(T), _ => new List<Delete>());
            list.Add(new Delete(typeof(T), id, entity));
        }

        public void Delete<T>(ValueType id)
        {
            delete<T>(id);
        }

        public void Delete<T>(string id)
        {
            delete<T>(id);
        }

        public void StoreUpdates<T>(params T[] documents)
        {
            var list = _updates.GetOrAdd(typeof (T), type => typeof (List<>).CloseAndBuildAs<IEnumerable>(typeof (T))).As<List<T>>();

            list.AddRange(documents);
        }

        public void StoreInserts<T>(params T[] documents)
        {
            var list = _inserts.GetOrAdd(typeof(T), type => typeof(List<>).CloseAndBuildAs<IEnumerable>(typeof(T))).As<List<T>>();

            list.AddRange(documents);
        }

        public IEnumerable<Delete> Deletions()
        {
            return _deletes.Values.SelectMany(x => x);
        }

        public IEnumerable<Delete> DeletionsFor<T>()
        {
            return Deletions().Where(x => x.DocumentType == typeof(T));
        }

        public IEnumerable<Delete> DeletionsFor(Type documentType)
        {
            return Deletions().Where(x => x.DocumentType == documentType);
        } 

        public IEnumerable<object> Updates()
        {
            return _updates.Values.SelectMany(x => x.OfType<object>())
                .Union(detectTrackerChanges().Select(x => x.Document));
        }

        public IEnumerable<T> UpdatesFor<T>()
        {
            var tracked =
                detectTrackerChanges()
                    .Where(x => x.DocumentType == typeof (T))
                    .Select(x => x.Document)
                    .OfType<T>()
                    .ToArray();

            if (_updates.ContainsKey(typeof (T)))
            {
                return _updates[typeof (T)].As<IList<T>>().Union(tracked).ToArray();
            }

            return tracked;
        }


        public ChangeSet ApplyChanges(UpdateBatch batch)
        {
            ChangeSet changes = buildChangeSet(batch);

            batch.Execute();

            ClearChanges(changes.Changes);

            return changes;
        }

        private ChangeSet buildChangeSet(UpdateBatch batch)
        {
            var documentChanges = GetChanges(batch);
            var changes = new ChangeSet(documentChanges);
            changes.Updated.Fill(Updates());
            changes.Inserted.Fill(Inserts());
            changes.Deleted.AddRange(Deletions());

            return changes;
        }

        public async Task<ChangeSet> ApplyChangesAsync(UpdateBatch batch, CancellationToken token)
        {
            var changes = buildChangeSet(batch);

            await batch.ExecuteAsync(token).ConfigureAwait(false);

            ClearChanges(changes.Changes);

            return changes;
        }

        private DocumentChange[] GetChanges(UpdateBatch batch)
        {
            _updates.Keys.Each(type =>
            {
                var storage = _schema.StorageFor(type);

                _updates[type].Each(o => storage.RegisterUpdate(batch, o));
            });

            _inserts.Keys.Each(type =>
            {
                var storage = _schema.StorageFor(type);

                _inserts[type].Each(o => storage.RegisterUpdate(batch, o));
            });

            _deletes.Keys.Each(type =>
            {
                var storage = _schema.StorageFor(type);
                var mapping = _schema.MappingFor(type);

                _deletes[type].Each(id => id.Configure(_parser, storage, mapping, batch));
            });

            var changes = detectTrackerChanges();
            changes.GroupBy(x => x.DocumentType).Each(group =>
            {
                var storage = _schema.StorageFor(group.Key);

                group.Each(c =>
                {
                    storage.RegisterUpdate(batch, c.Document, c.Json);
                });
            });

            return changes;
        }

        private DocumentChange[] detectTrackerChanges()
        {
            return _trackers.SelectMany(x => x.DetectChanges()).ToArray();
        }

        private void ClearChanges(DocumentChange[] changes)
        {
            _deletes.Clear();
            _updates.Clear();
            _inserts.Clear();
            changes.Each(x => x.ChangeCommitted());
        }

        public IEnumerable<object> Inserts()
        {
            return _inserts.Values.SelectMany(x => x.OfType<object>());
        }

        public IEnumerable<T> InsertsFor<T>()
        {
            if (_inserts.ContainsKey(typeof(T)))
            {
                return _inserts[typeof (T)].As<IList<T>>();
            }

            return Enumerable.Empty<T>();
        }

        public IEnumerable<T> AllChangedFor<T>()
        {
            return InsertsFor<T>().Union(UpdatesFor<T>());
        }

        public void Delete<T>(Expression<Func<T, bool>> expression)
        {
            var delete = new Delete(typeof(T), expression);
            var list = _deletes.GetOrAdd(typeof(T), _ => new List<Delete>());
            list.Add(delete);
        }
    }
}