using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LamarCodeGeneration;
using LamarCodeGeneration.Frames;
using LamarCodeGeneration.Model;

namespace Marten.Events.V4Concept.CodeGeneration
{
    internal class CreateMethodCollection : MethodCollection
    {
        public Type AggregateType { get; }
        public static readonly string MethodName = "Create";

        public CreateMethodCollection(Type projectionType, Type aggregateType) : base(MethodName, projectionType)
        {
            AggregateType = aggregateType;
        }

        public void BuildCreateMethod(GeneratedType generatedType)
        {
            var returnType = IsAsync
                ? typeof(ValueTask<>).MakeGenericType(AggregateType)
                : AggregateType;

            var args = new[] {new Argument(typeof(IEvent), "@event"), new Argument(typeof(IQuerySession), "session")};
            if (IsAsync)
            {
                args = args.Concat(new[] {new Argument(typeof(CancellationToken), "cancellation")}).ToArray();
            }

            var method = new GeneratedMethod(MethodName, returnType, args);
            generatedType.AddMethod(method);

            var frames = AddEventHandling(AggregateType, this);
            method.Frames.AddRange(frames);


            method.Frames.Add(new DefaultAggregateConstruction(AggregateType)
                {IfStyle = Methods.Any() ? IfStyle.Else : IfStyle.None});
        }


        public override IEventHandlingFrame CreateAggregationHandler(Type aggregateType, MethodInfo method)
        {
            return new CreateAggregateFrame(ProjectionType, method);
        }
    }
}
