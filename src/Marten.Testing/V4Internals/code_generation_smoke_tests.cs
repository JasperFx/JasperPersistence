using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Baseline;
using Marten.Schema;
using Marten.Services;
using Marten.Storage;
using Marten.Testing.CoreFunctionality;
using Marten.Testing.Documents;
using Marten.Testing.Harness;
using Marten.V4Internals;
using NSubstitute;
using Xunit;
using VersionTracker = Marten.V4Internals.VersionTracker;

namespace Marten.Testing.V4Internals
{
    public interface ICodeGenScenario
    {
        void Compile();
        void CanGetIdentity();

        void HasQueryOnlyStorage();
        void HasLightweightStorage();
        void HasIdentityMapStorage();
        void HasDirtyTrackingStorage();

        void CanStoreLightweight();
        void CanStoreIdentityMap();
        //void CanStoreDirtyChecking();
        void CanEjectLightweight();
        void CanEjectIdentityMap();
    }

    public class StubMartenSession: IMartenSession
    {
        public ISerializer Serializer { get; } = new JsonNetSerializer();
        public Dictionary<Type, object> ItemMap { get; } = new Dictionary<Type, object>();
        public ITenant Tenant { get; } = Substitute.For<ITenant>();
        public VersionTracker Versions { get; } = new VersionTracker();
        public Task<T> ExecuteQuery<T>(IQueryHandler<T> handler, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public T ExecuteQuery<T>(IQueryHandler<T> handler)
        {
            throw new NotImplementedException();
        }

        public IQueryable CreateQuery(Expression expression)
        {
            throw new NotImplementedException();
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            throw new NotImplementedException();
        }

        public object Execute(Expression expression)
        {
            throw new NotImplementedException();
        }

        public TResult Execute<TResult>(Expression expression)
        {
            throw new NotImplementedException();
        }
    }

    public class CodeGenScenario<T> : ICodeGenScenario where T : new()
    {
        public string Description { get; }
        public DocumentMapping<T> Mapping { get; }
        public StoreOptions Options { get; }

        public CodeGenScenario(string description, DocumentMapping<T> mapping, StoreOptions options)
        {
            Description = description;
            Mapping = mapping;
            Options = options;

            Document = new T();
        }

        public T Document { get; set; }

        public override string ToString()
        {
            return Description;
        }

        public void Compile()
        {
            CreateSlot();
        }

        public void CanGetIdentity()
        {
            var storage = CreateSlot().QueryOnly;
            var identityMethod = storage.GetType().GetMethod("Identity");
            identityMethod.Invoke(storage, new object[] {Document});
        }

        public void HasQueryOnlyStorage()
        {
            CreateSlot().QueryOnly.ShouldNotBeNull();
        }

        public void HasLightweightStorage()
        {
            CreateSlot().Lightweight.ShouldNotBeNull();
        }

        public void HasIdentityMapStorage()
        {
            CreateSlot().IdentityMap.ShouldNotBeNull();
        }

        public void HasDirtyTrackingStorage()
        {
            CreateSlot().DirtyTracking.ShouldNotBeNull();
        }

        public void CanStoreLightweight()
        {
            CreateSlot().Lightweight.Store(new StubMartenSession(), Document);
            CreateSlot().Lightweight.Store(new StubMartenSession(), Document, Guid.NewGuid());
        }

        public void CanEjectLightweight()
        {
            var session = new StubMartenSession();
            CreateSlot().Lightweight.Store(session, Document);
            CreateSlot().Lightweight.Eject(session, Document);
        }

        public void CanEjectIdentityMap()
        {
            var session = new StubMartenSession();
            CreateSlot().IdentityMap.Store(session, Document);
            CreateSlot().IdentityMap.Eject(session, Document);
        }

        public void CanStoreIdentityMap()
        {
            CreateSlot().IdentityMap.Store(new StubMartenSession(), Document);
            CreateSlot().IdentityMap.Store(new StubMartenSession(), Document, Guid.NewGuid());
        }

        private StorageSlot<T> CreateSlot()
        {
            var builder = new DocumentStorageBuilder(Mapping, Options);
            return builder.Generate<T>();
        }
    }

    public class code_generation_smoke_tests
    {
        private static readonly IList<ICodeGenScenario> _scenarios = new List<ICodeGenScenario>();

        protected static T Scenario<T>(string description, Func<StoreOptions, MartenRegistry.DocumentMappingExpression<T>> configuration = null) where T : new()
        {
            var options = new StoreOptions();
            var expression = configuration(options);

            options.ApplyConfiguration();

            var mapping = options.Storage.MappingFor(typeof(T)).As<DocumentMapping<T>>();
            var scenario = new CodeGenScenario<T>(description, mapping, options);

            _scenarios.Add(scenario);

            return scenario.Document;
        }


        static code_generation_smoke_tests()
        {
            Scenario("Guid Id", x => x.Schema.For<Target>());
            Scenario("Int Id", x => x.Schema.For<IntDoc>());
            Scenario("Long Id", x => x.Schema.For<LongDoc>());
            Scenario("String Id", x => x.Schema.For<StringDoc>())
                .Id = "foo";

        }

        public static IEnumerable<object[]> TestCases()
        {
            return testCases().ToArray();
        }

        private static IEnumerable<object[]> testCases()
        {
            var methods = typeof(ICodeGenScenario).GetMethods();

            foreach (var scenario in _scenarios)
            {
                foreach (var method in methods)
                {
                    yield return new object[]{scenario, method};
                }
            }
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void compilation(ICodeGenScenario scenario, MethodInfo method)
        {
            method.Invoke(scenario, new object[0]);
        }




    }
}
