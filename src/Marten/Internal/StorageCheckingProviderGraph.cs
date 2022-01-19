using System;
using Baseline;
using Baseline.ImTools;
using Marten.Internal.CodeGeneration;
using Marten.Storage;
using Marten.Util;
#nullable enable
namespace Marten.Internal
{
    public class StorageCheckingProviderGraph: IProviderGraph
    {
        private ImHashMap<Type, object> _storage = ImHashMap<Type, object>.Empty;
        private readonly IProviderGraph _inner;

        public StorageCheckingProviderGraph(IMartenDatabase tenant, IProviderGraph inner)
        {
            Tenant = tenant;
            _inner = inner;
        }

        public IMartenDatabase Tenant { get; }

        public DocumentProvider<T> StorageFor<T>() where T : notnull
        {
            if (_storage.TryFind(typeof(T), out var stored))
            {
                return stored.As<DocumentProvider<T>>();
            }

            Tenant.EnsureStorageExists(typeof(T));
            var persistence = _inner.StorageFor<T>();

            _storage = _storage.AddOrUpdate(typeof(T), persistence);

            return persistence;
        }

        public void Append<T>(DocumentProvider<T> provider)
        {
            // This might cause Marten to re-check the database storage, but double dipping
            // seems smarter than trying to be too clever and miss doing the check
            _storage = _storage.Remove(typeof(T));
            _inner.Append(provider);
        }
    }
}
