using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamarCodeGeneration;
using Marten.Exceptions;
using Marten.Internal.Storage;

#nullable enable

namespace Marten.Internal.Sessions
{
    public partial class QuerySession
    {
        public T? Load<T>(string id) where T : notnull
        {
            assertNotDisposed();
            var document = StorageFor<T, string>().Load(id, this);

            return document;
        }

        public async Task<T?> LoadAsync<T>(string id, CancellationToken token = default) where T : notnull
        {
            assertNotDisposed();
            var document = await StorageFor<T, string>().LoadAsync(id, this, token).ConfigureAwait(false);

            return document;
        }

        public T? Load<T>(int id) where T : notnull
        {
            assertNotDisposed();

            var storage = StorageFor<T>();

            var document = storage switch
            {
                IDocumentStorage<T, int> i => i.Load(id, this),
                IDocumentStorage<T, long> l => l.Load(id, this),
                _ => throw new DocumentIdTypeMismatchException(
                    $"The identity type for document type {typeof(T).FullNameInCode()} is not numeric")
            };

            return document;
        }

        public async Task<T?> LoadAsync<T>(int id, CancellationToken token = default) where T : notnull
        {
            assertNotDisposed();

            var storage = StorageFor<T>();

            var document = storage switch
            {
                IDocumentStorage<T, int> i => await i.LoadAsync(id, this, token).ConfigureAwait(false),
                IDocumentStorage<T, long> l => await l.LoadAsync(id, this, token).ConfigureAwait(false),
                _ => throw new DocumentIdTypeMismatchException(
                    $"The identity type for document type {typeof(T).FullNameInCode()} is not numeric")
            };

            return document;
        }

        public T? Load<T>(long id) where T : notnull
        {
            assertNotDisposed();
            var document = StorageFor<T, long>().Load(id, this);

            return document;
        }

        public async Task<T?> LoadAsync<T>(long id, CancellationToken token = default) where T : notnull
        {
            assertNotDisposed();
            var document = await StorageFor<T, long>().LoadAsync(id, this, token).ConfigureAwait(false);

            return document;
        }

        public T? Load<T>(Guid id) where T : notnull
        {
            assertNotDisposed();
            var document = StorageFor<T, Guid>().Load(id, this);

            return document;
        }

        public async Task<T?> LoadAsync<T>(Guid id, CancellationToken token = default) where T : notnull
        {
            assertNotDisposed();
            var document = await StorageFor<T, Guid>().LoadAsync(id, this, token).ConfigureAwait(false);

            return document;
        }

        public IReadOnlyList<T> LoadMany<T>(params string[] ids) where T : notnull
        {
            assertNotDisposed();
            return StorageFor<T, string>().LoadMany(ids, this);
        }

        public IReadOnlyList<T> LoadMany<T>(IEnumerable<string> ids) where T : notnull
        {
            assertNotDisposed();
            return StorageFor<T, string>().LoadMany(ids.ToArray(), this);

        }

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(params string[] ids) where T : notnull
        {
            assertNotDisposed();
            return StorageFor<T, string>().LoadManyAsync(ids, this, default);

        }

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(IEnumerable<string> ids) where T : notnull
        {
            assertNotDisposed();
            return StorageFor<T, string>().LoadManyAsync(ids.ToArray(), this, default);
        }

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(CancellationToken token, params string[] ids) where T : notnull
        {
            assertNotDisposed();
            return StorageFor<T, string>().LoadManyAsync(ids, this, token);
        }

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(CancellationToken token, IEnumerable<string> ids) where T : notnull
        {
            assertNotDisposed();
            return StorageFor<T, string>().LoadManyAsync(ids.ToArray(), this, token);
        }

        public IReadOnlyList<T> LoadMany<T>(params int[] ids) where T : notnull
        {
            assertNotDisposed();

            var storage = StorageFor<T>();
            if (storage is IDocumentStorage<T, int> i)
            {
                return i.LoadMany(ids, this);
            }
            else if (storage is IDocumentStorage<T, long> l)
            {
                return l.LoadMany(ids.Select(x => (long)x).ToArray(), this);
            }


            throw new DocumentIdTypeMismatchException($"The identity type for document type {typeof(T).FullNameInCode()} is not numeric");
        }

        public IReadOnlyList<T> LoadMany<T>(IEnumerable<int> ids) where T : notnull
        {
            return LoadMany<T>(ids.ToArray());
        }

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(params int[] ids) where T : notnull
        {
            return LoadManyAsync<T>(CancellationToken.None, ids);
        }

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(IEnumerable<int> ids) where T : notnull
        {
            return LoadManyAsync<T>(ids.ToArray());
        }

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(CancellationToken token, params int[] ids) where T : notnull
        {
            assertNotDisposed();

            var storage = StorageFor<T>();
            if (storage is IDocumentStorage<T, int> i)
            {
                return i.LoadManyAsync(ids, this, token);
            }
            else if (storage is IDocumentStorage<T, long> l)
            {
                return l.LoadManyAsync(ids.Select(x => (long)x).ToArray(), this, token);
            }


            throw new DocumentIdTypeMismatchException($"The identity type for document type {typeof(T).FullNameInCode()} is not numeric");
        }

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(CancellationToken token, IEnumerable<int> ids) where T : notnull
        {
            return LoadManyAsync<T>(token, ids.ToArray());
        }

        public IReadOnlyList<T> LoadMany<T>(params long[] ids) where T : notnull
        {
            assertNotDisposed();
            return StorageFor<T, long>().LoadMany(ids, this);
        }

        public IReadOnlyList<T> LoadMany<T>(IEnumerable<long> ids) where T : notnull
        {
            assertNotDisposed();
            return StorageFor<T, long>().LoadMany(ids.ToArray(), this);

        }

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(params long[] ids) where T : notnull
        {
            assertNotDisposed();
            return StorageFor<T, long>().LoadManyAsync(ids, this, default);

        }

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(IEnumerable<long> ids) where T : notnull
        {
            assertNotDisposed();
            return StorageFor<T, long>().LoadManyAsync(ids.ToArray(), this, default);
        }

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(CancellationToken token, params long[] ids) where T : notnull
        {
            assertNotDisposed();
            return StorageFor<T, long>().LoadManyAsync(ids, this, token);
        }

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(CancellationToken token, IEnumerable<long> ids) where T : notnull
        {
            assertNotDisposed();
            return StorageFor<T, long>().LoadManyAsync(ids.ToArray(), this, token);
        }

        public IReadOnlyList<T> LoadMany<T>(params Guid[] ids) where T: notnull
        {
            assertNotDisposed();
            return StorageFor<T, Guid>().LoadMany(ids, this);
        }

        public IReadOnlyList<T> LoadMany<T>(IEnumerable<Guid> ids) where T : notnull
        {
            assertNotDisposed();
            return StorageFor<T, Guid>().LoadMany(ids.ToArray(), this);

        }

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(params Guid[] ids) where T : notnull
        {
            assertNotDisposed();
            return StorageFor<T, Guid>().LoadManyAsync(ids, this, default);

        }

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(IEnumerable<Guid> ids) where T : notnull
        {
            assertNotDisposed();
            return StorageFor<T, Guid>().LoadManyAsync(ids.ToArray(), this, default);
        }

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(CancellationToken token, params Guid[] ids) where T : notnull
        {
            assertNotDisposed();
            return StorageFor<T, Guid>().LoadManyAsync(ids, this, token);
        }

        public Task<IReadOnlyList<T>> LoadManyAsync<T>(CancellationToken token, IEnumerable<Guid> ids) where T : notnull
        {
            assertNotDisposed();
            return StorageFor<T, Guid>().LoadManyAsync(ids.ToArray(), this, token);
        }

    }
}
