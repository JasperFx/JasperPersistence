using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using LamarCodeGeneration;
using LamarCompiler;
using Marten.Internal.Storage;
using Weasel.Postgresql;
using Marten.Schema;
using Marten.Schema.Arguments;
using Marten.Schema.BulkLoading;
using Marten.Util;
using Npgsql;
using CommandExtensions = Weasel.Postgresql.CommandExtensions;

namespace Marten.Internal.CodeGeneration
{
    public class DocumentPersistenceBuilder
    {
        private readonly DocumentMapping _mapping;
        private readonly StoreOptions _options;

        public DocumentPersistenceBuilder(DocumentMapping mapping, StoreOptions options)
        {
            _mapping = mapping;
            _options = options;
        }

        public void AssemblyTypes(GeneratedAssembly assembly)
        {
            var operations = new DocumentOperations(assembly, _mapping, _options);

            assembly.Namespaces.Add(typeof(CommandExtensions).Namespace);
            assembly.Namespaces.Add(typeof(TenantIdArgument).Namespace);
            assembly.Namespaces.Add(typeof(NpgsqlCommand).Namespace);
            assembly.Namespaces.Add(typeof(Weasel.Core.CommandExtensions).Namespace);


            new DocumentStorageBuilder(_mapping, StorageStyle.QueryOnly, x => x.QueryOnlySelector)
                .Build(assembly, operations);

            new DocumentStorageBuilder(_mapping, StorageStyle.Lightweight, x => x.LightweightSelector)
                .Build(assembly, operations);

            new DocumentStorageBuilder(_mapping, StorageStyle.IdentityMap, x => x.IdentityMapSelector)
                .Build(assembly, operations);

            new DocumentStorageBuilder(_mapping, StorageStyle.DirtyTracking, x => x.DirtyCheckingSelector)
                .Build(assembly, operations);

            new BulkLoaderBuilder(_mapping).BuildType(assembly);
        }

        public static DocumentProvider<T> FromPreBuiltTypes<T>(Assembly assembly, DocumentMapping mapping)
        {
            var queryOnly = assembly.ExportedTypes.FirstOrDefault(x =>
                x.Name == DocumentStorageBuilder.DeriveTypeName(mapping, StorageStyle.QueryOnly));

            var lightweight = assembly.ExportedTypes.FirstOrDefault(x =>
                x.Name == DocumentStorageBuilder.DeriveTypeName(mapping, StorageStyle.Lightweight));

            var identityMap = assembly.ExportedTypes.FirstOrDefault(x =>
                x.Name == DocumentStorageBuilder.DeriveTypeName(mapping, StorageStyle.IdentityMap));

            var dirtyTracking = assembly.ExportedTypes.FirstOrDefault(x =>
                x.Name == DocumentStorageBuilder.DeriveTypeName(mapping, StorageStyle.DirtyTracking));

            var bulkWriterType =
                assembly.ExportedTypes.FirstOrDefault(x => x.Name == new BulkLoaderBuilder(mapping).TypeName);

            var slot = new DocumentProvider<T>
            {
                QueryOnly = (IDocumentStorage<T>)Activator.CreateInstance(queryOnly, mapping),
                Lightweight = (IDocumentStorage<T>)Activator.CreateInstance(lightweight, mapping),
                IdentityMap = (IDocumentStorage<T>)Activator.CreateInstance(identityMap, mapping),
                DirtyTracking = (IDocumentStorage<T>)Activator.CreateInstance(dirtyTracking, mapping),

            };

            slot.BulkLoader = mapping.IsHierarchy()
                ? (IBulkLoader<T>)Activator.CreateInstance(bulkWriterType, slot.QueryOnly, mapping)
                : (IBulkLoader<T>)Activator.CreateInstance(bulkWriterType, slot.QueryOnly);


            return slot;
        }

        public DocumentProvider<T> Generate<T>()
        {
            var assembly = new GeneratedAssembly(new GenerationRules(SchemaConstants.MartenGeneratedNamespace));

            var operations = new DocumentOperations(assembly, _mapping, _options);

            assembly.Namespaces.Add(typeof(CommandExtensions).Namespace);
            assembly.Namespaces.Add(typeof(Weasel.Core.CommandExtensions).Namespace);
            assembly.Namespaces.Add(typeof(TenantIdArgument).Namespace);
            assembly.Namespaces.Add(typeof(NpgsqlCommand).Namespace);


            var queryOnly = new DocumentStorageBuilder(_mapping, StorageStyle.QueryOnly, x => x.QueryOnlySelector)
                .Build(assembly, operations);

            var lightweight = new DocumentStorageBuilder(_mapping, StorageStyle.Lightweight, x => x.LightweightSelector)
                .Build(assembly, operations);

            var identityMap = new DocumentStorageBuilder(_mapping, StorageStyle.IdentityMap, x => x.IdentityMapSelector)
                .Build(assembly, operations);

            var dirtyTracking = new DocumentStorageBuilder(_mapping, StorageStyle.DirtyTracking, x => x.DirtyCheckingSelector)
                .Build(assembly, operations);

            var bulkWriterType = new BulkLoaderBuilder(_mapping).BuildType(assembly);

            var compiler = new AssemblyGenerator();

            var types = new[]
            {
                typeof(IDocumentStorage<>),
                typeof(T),
            };

            foreach (var referencedAssembly in WalkReferencedAssemblies.ForTypes(types))
            {
                compiler.ReferenceAssembly(referencedAssembly);
            }

            try
            {
                compiler.Compile(assembly);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("is inaccessible due to its protection level"))
                {
                    throw new InvalidOperationException($"Requested document type '{_mapping.DocumentType.FullNameInCode()}' must be either scoped as 'public' or the assembly holding it must use the {nameof(InternalsVisibleToAttribute)} pointing to 'Marten.Generated'", e);
                }

                throw;
            }

            var slot = new DocumentProvider<T>
            {
                QueryOnly = (IDocumentStorage<T>)Activator.CreateInstance(queryOnly.CompiledType, _mapping),
                Lightweight = (IDocumentStorage<T>)Activator.CreateInstance(lightweight.CompiledType, _mapping),
                IdentityMap = (IDocumentStorage<T>)Activator.CreateInstance(identityMap.CompiledType, _mapping),
                DirtyTracking = (IDocumentStorage<T>)Activator.CreateInstance(dirtyTracking.CompiledType, _mapping),

                Operations = operations,
                QueryOnlyType = queryOnly,
                LightweightType = lightweight,
                IdentityMapType = identityMap,
                DirtyTrackingType = dirtyTracking
            };

            slot.BulkLoader = _mapping.IsHierarchy()
                ? (IBulkLoader<T>)Activator.CreateInstance(bulkWriterType.CompiledType, slot.QueryOnly, _mapping)
                : (IBulkLoader<T>)Activator.CreateInstance(bulkWriterType.CompiledType, slot.QueryOnly);

            slot.BulkLoaderType = bulkWriterType;

            return slot;
        }
    }
}
