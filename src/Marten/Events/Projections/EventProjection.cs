using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Baseline;
using LamarCodeGeneration;
using LamarCodeGeneration.Frames;
using LamarCodeGeneration.Model;
using LamarCompiler;
using Marten.Events.CodeGeneration;
using Marten.Events.Daemon;
using Marten.Exceptions;
using Marten.Internal;
using Marten.Internal.CodeGeneration;
using Marten.Linq.SqlGeneration;
using Marten.Schema;
using Marten.Storage;
using Weasel.Postgresql.SqlGeneration;

namespace Marten.Events.Projections
{


    /// <summary>
    /// This is the "do anything" projection type
    /// </summary>
    public abstract class EventProjection : ProjectionSource
    {
        private readonly ProjectMethodCollection _projectMethods;
        private readonly CreateMethodCollection _createMethods;

        public EventProjection() : base("Projections")
        {
            _projectMethods = new ProjectMethodCollection(GetType());
            _createMethods = new CreateMethodCollection(GetType());

            ProjectionName = GetType().FullNameInCode();
        }

        public override Type ProjectionType => GetType();

        internal override void AssertValidity()
        {
            if (!_projectMethods.Methods.Any() && !_createMethods.Methods.Any())
            {
                throw new InvalidProjectionException(
                    $"EventProjection {GetType().FullNameInCode()} has no valid projection operations");
            }

            var invalidMethods = MethodCollection.FindInvalidMethods(GetType(), _projectMethods, _createMethods);

            if (invalidMethods.Any())
            {
                throw new InvalidProjectionException(this, invalidMethods);
            }
        }

        [MartenIgnore]
        public void Project<TEvent>(Action<TEvent, IDocumentOperations> project)
        {
            _projectMethods.AddLambda(project, typeof(TEvent));
        }

        [MartenIgnore]
        public void ProjectAsync<TEvent>(Func<TEvent, IDocumentOperations, Task> project)
        {
            _projectMethods.AddLambda(project, typeof(TEvent));
        }

        /// <summary>
        /// This would be a helper for the open ended EventProjection
        /// </summary>
        internal class ProjectMethodCollection: MethodCollection
        {
            public static readonly string MethodName = "Project";


            public ProjectMethodCollection(Type projectionType) : base(MethodName, projectionType, null)
            {
                _validArgumentTypes.Add(typeof(IDocumentOperations));
                _validReturnTypes.Add(typeof(void));
                _validReturnTypes.Add(typeof(Task));
            }

            internal override void validateMethod(MethodSlot method)
            {
                if (method.Method.GetParameters().All(x => x.ParameterType != typeof(IDocumentOperations)))
                {
                    method.AddError($"{typeof(IDocumentOperations).FullNameInCode()} is a required parameter");
                }
            }

            public override IEventHandlingFrame CreateEventTypeHandler(Type aggregateType,
                IDocumentMapping aggregateMapping,
                MethodSlot slot)
            {
                return new ProjectMethodCall(slot);
            }
        }

        internal class ProjectMethodCall: MethodCall, IEventHandlingFrame
        {
            public ProjectMethodCall(MethodSlot slot) : base(slot.HandlerType, (MethodInfo) slot.Method)
            {
                EventType = Method.GetEventType(null);
                Target = slot.Setter;
            }

            public Type EventType { get; }

            public void Configure(EventProcessingFrame parent)
            {
                // Replace any arguments to IEvent<T>

                TrySetArgument(parent.SpecificEvent);

                // Replace any arguments to the specific T event type
                TrySetArgument(parent.DataOnly);
            }
        }

        /// <summary>
        /// This would be a helper for the open ended EventProjection
        /// </summary>
        internal class CreateMethodCollection: MethodCollection
        {
            public static readonly string MethodName = "Create";
            public static readonly string TransformMethodName = "Transform";

            public CreateMethodCollection(Type projectionType): base(new[] { MethodName, TransformMethodName}, projectionType, null)
            {
                _validArgumentTypes.Add(typeof(IDocumentOperations));
            }

            public override IEventHandlingFrame CreateEventTypeHandler(Type aggregateType,
                IDocumentMapping aggregateMapping,
                MethodSlot slot)
            {
                return new CreateMethodFrame(slot);
            }

            internal override void validateMethod(MethodSlot method)
            {
                if (method.ReturnType == typeof(void))
                {
                    method.AddError($"The return value must be a new document");
                }
            }
        }

        internal class CreateMethodFrame: MethodCall, IEventHandlingFrame
        {
            private static int _counter = 0;

            private Variable _operations;

            public CreateMethodFrame(MethodSlot slot) : base(slot.HandlerType, (MethodInfo) slot.Method)
            {
                EventType = Method.GetEventType(null);
                ReturnVariable.OverrideName(ReturnVariable.Usage + ++_counter);
            }

            public Type EventType { get; }

            public void Configure(EventProcessingFrame parent)
            {
                // Replace any arguments to IEvent<T>
                TrySetArgument(parent.SpecificEvent);

                // Replace any arguments to the specific T event type
                TrySetArgument(parent.DataOnly);
            }

            public override IEnumerable<Variable> FindVariables(IMethodVariables chain)
            {
                foreach (var variable in base.FindVariables(chain))
                {
                    yield return variable;
                }

                _operations = chain.FindVariable(typeof(IDocumentOperations));

            }

            public override void GenerateCode(GeneratedMethod method, ISourceWriter writer)
            {
                base.GenerateCode(method, writer);
                writer.WriteLine($"{_operations.Usage}.Store({ReturnVariable.Usage});");
            }
        }

        private GeneratedType _inlineType;
        private GeneratedAssembly _assembly;
        private bool _isAsync;

        internal override IProjection Build(DocumentStore store)
        {
            if (_inlineType == null)
            {
                Compile();
            }

            var inline = (IProjection)Activator.CreateInstance(_inlineType.CompiledType, this);
            _inlineType.ApplySetterValues(inline);

            return inline;
        }

        internal override IReadOnlyList<AsyncProjectionShard> AsyncProjectionShards(DocumentStore store)
        {
            var baseFilters = new ISqlFragment[0];
            var eventTypes = MethodCollection.AllEventTypes(_createMethods, _projectMethods);
            if (!eventTypes.Any(x => x.IsAbstract || x.IsInterface))
            {
                baseFilters = new ISqlFragment[] {new Marten.Events.Daemon.EventTypeFilter(store.Events, eventTypes)};
            }

            return new List<AsyncProjectionShard> {new(this, baseFilters)};
        }

        internal void Compile()
        {
            _assembly = new GeneratedAssembly(new GenerationRules(SchemaConstants.MartenGeneratedNamespace));

            _assembly.Rules.Assemblies.Add(GetType().Assembly);
            _assembly.Rules.Assemblies.AddRange(_projectMethods.ReferencedAssemblies());
            _assembly.Rules.Assemblies.AddRange(_createMethods.ReferencedAssemblies());

            _assembly.UsingNamespaces.Add("System.Linq");

            _isAsync = _createMethods.IsAsync || _projectMethods.IsAsync;

            var baseType = _isAsync ? typeof(AsyncEventProjection<>) : typeof(SyncEventProjection<>);
            baseType = baseType.MakeGenericType(GetType());
            _inlineType = _assembly.AddType(GetType().Name.Sanitize() + "GeneratedInlineProjection", baseType);

            var method = _inlineType.MethodFor("ApplyEvent");
            method.DerivedVariables.Add(new Variable(GetType(), "Projection"));

            var eventHandling = MethodCollection.AddEventHandling(null, null, _createMethods, _projectMethods);
            method.Frames.Add(eventHandling);

            var assemblyGenerator = new AssemblyGenerator();

            assemblyGenerator.ReferenceAssembly(typeof(IMartenSession).Assembly);
            assemblyGenerator.Compile(_assembly);

            Debug.WriteLine(_inlineType.SourceCode);
        }

        protected override IEnumerable<Type> publishedTypes()
        {
            foreach (var method in _createMethods.Methods)
            {
                var docType = method.ReturnType;
                if (docType.Closes(typeof(Task<>)))
                {
                    yield return docType.GetGenericArguments().Single();
                }
                else
                {
                    yield return docType;
                }
            }
        }
    }

    public abstract class SyncEventProjection<T>: SyncEventProjectionBase where T : EventProjection
    {
        public T Projection { get; }

        public SyncEventProjection(T projection)
        {
            Projection = projection;
        }
    }

    public abstract class AsyncEventProjection<T> : AsyncEventProjectionBase where T : EventProjection
    {
        public T Projection { get; }

        public AsyncEventProjection(T projection)
        {
            Projection = projection;
        }



    }

}
