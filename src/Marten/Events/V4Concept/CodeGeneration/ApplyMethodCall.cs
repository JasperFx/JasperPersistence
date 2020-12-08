using System;
using System.Reflection;
using LamarCodeGeneration.Frames;
using Marten.Events.Projections.Async;

namespace Marten.Events.V4Concept.CodeGeneration
{
    internal class ApplyMethodCall: MethodCall, IEventHandlingFrame
    {
        public ApplyMethodCall(Type handlerType, string methodName) : base(handlerType, methodName)
        {
            EventType = AggregationTypeBuilder.EventTypeFor(Method);
        }

        public ApplyMethodCall(Type handlerType, MethodInfo method) : base(handlerType, method)
        {
            EventType = AggregationTypeBuilder.EventTypeFor(Method);
        }

        public Type EventType { get; }

        public void Configure(EventProcessingFrame parent)
        {
            // Replace any arguments to Event<T>
            TrySetArgument(parent.SpecificEvent);

            // Replace any arguments to the specific T event type
            TrySetArgument(parent.DataOnly);

            if (ReturnType == parent.AggregateType)
            {
                AssignResultTo(parent.Aggregate);
            }
        }
    }
}
