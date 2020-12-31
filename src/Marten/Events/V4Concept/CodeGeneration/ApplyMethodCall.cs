using System;
using LamarCodeGeneration.Frames;

namespace Marten.Events.V4Concept.CodeGeneration
{
    internal class ApplyMethodCall: MethodCall, IEventHandlingFrame
    {
        public ApplyMethodCall(Type handlerType, string methodName, Type aggregateType) : base(handlerType, methodName)
        {
            EventType = Method.GetEventType(aggregateType);
        }

        public ApplyMethodCall(Type handlerType, MethodSlot slot) : base(handlerType, slot.Method)
        {
            EventType = slot.EventType;
            if (slot.Setter != null)
            {
                Target = slot.Setter;
            }
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
