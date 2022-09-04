using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Baseline;
using Baseline.Dates;
using Marten.AsyncDaemon.Testing.TestingSupport;
using Marten.Events;
using Marten.Events.Daemon;
using Marten.Events.Projections;
using Marten.Exceptions;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Marten.AsyncDaemon.Testing.Resiliency;

public class error_handling : DaemonContext
{
    public error_handling(ITestOutputHelper output) : base(output)
    {
    }

    [Fact] // Rerun this test if it fails. I think that thread exhaustion nails this sometimes
    public async Task projections_can_continue_with_handled_exceptions()
    {
        var projection1 = new SometimesFailingProjection("one");
        projection1.StartThrowingExceptionsAtSequence(10, new ArithmeticException(), new ArithmeticException());

        StoreOptions(opts =>
        {
            opts.Projections.Add(projection1, ProjectionLifecycle.Async);
            opts.Projections.OnException<ArithmeticException>()
                .RetryLater(50.Milliseconds(), 50.Milliseconds());

        });

        using var node = await StartDaemon();

        NumberOfStreams = 10;
        await PublishSingleThreaded();
        await node.Tracker.WaitForHighWaterMark(NumberOfEvents);

        var i = 0;
        while (i < 10)
        {
            if (node.StatusFor("one:All") == AgentStatus.Running) break;
            await Task.Delay(1.Seconds());
            i++;
        }
        node.StatusFor("one:All").ShouldBe(AgentStatus.Running);
    }

    [Fact]
    public async Task projections_can_continue_with_handled_exceptions_after_a_pause()
    {
        // WARNING -- this test doesn't consistently pass if it's running with other tests,
        // but should succeed running by itself.

        var projection1 = new SometimesFailingProjection("one");
        projection1.StartThrowingExceptionsAtSequence(10, new ArithmeticException(), new ArithmeticException(), new ArithmeticException());

        StoreOptions(opts =>
        {
            opts.Projections.Add(projection1, ProjectionLifecycle.Async);
            opts.Projections
                .OnException<ApplyEventException>()
                .AndInner<ArithmeticException>()
                .RetryLater(50.Milliseconds(), 50.Milliseconds())
                .Then.Pause(50.Milliseconds());
        });

        using var node = await StartDaemon();

        var waiter = node.Tracker.WaitForShardCondition(
            state => state.ShardName.EqualsIgnoreCase("one:All") && state.Action == ShardAction.Paused,
            "one:All is Paused", 5.Minutes());

        NumberOfStreams = 10;
        await PublishSingleThreaded();

        await waiter;
        await node.Tracker.WaitForShardState("one:All", NumberOfEvents);

        if (node.StatusFor("one:All") != AgentStatus.Running)
        {
            await Task.Delay(250.Milliseconds());
            node.StatusFor("one:All").ShouldBe(AgentStatus.Running);
        }


    }

    [Fact]
    public async Task all_projections_can_continue_with_handled_exceptions_after_a_pause()
    {
        var projection1 = new SometimesFailingProjection("one");
        projection1.StartThrowingExceptionsAtSequence(10, new ArithmeticException(), new ArithmeticException(), new ArithmeticException());

        var projection2 = new SometimesFailingProjection("two");

        StoreOptions(opts =>
        {
            opts.Projections.Add(projection1, ProjectionLifecycle.Async);
            opts.Projections.Add(projection2, ProjectionLifecycle.Async);
            opts.Projections.OnException<ApplyEventException>().AndInner<ArithmeticException>()
                .RetryLater(50.Milliseconds(), 50.Milliseconds()).Then.PauseAll(50.Milliseconds());
        });

        using var node = await StartDaemon();

        var waiter1 = node.Tracker.WaitForShardCondition(
            state => state.ShardName.EqualsIgnoreCase("one:All") && state.Action == ShardAction.Paused,
            "one:All is Paused", 1.Minutes());

        var waiter2 = node.Tracker.WaitForShardCondition(
            state => state.ShardName.EqualsIgnoreCase("one:All") && state.Action == ShardAction.Paused,
            "one:All is Paused", 1.Minutes());

        NumberOfStreams = 10;
        await PublishSingleThreaded();

        await waiter1;
        await waiter2;
        await node.Tracker.WaitForShardState("one:All", NumberOfEvents);

        if (node.StatusFor("one:All") != AgentStatus.Running)
        {
            await Task.Delay(250.Milliseconds());
        }

        node.StatusFor("one:All").ShouldBe(AgentStatus.Running);
        node.StatusFor("two:All").ShouldBe(AgentStatus.Running);
    }


    [Fact]
    public async Task projections_stops_on_too_many_tries()
    {
        var projection1 = new SometimesFailingProjection("one");
        projection1.StartThrowingExceptionsAtSequence(10, new ArithmeticException(), new ArithmeticException(), new ArithmeticException(), new ArithmeticException());

        StoreOptions(opts =>
        {
            opts.Projections.Add(projection1, ProjectionLifecycle.Async);
            opts.Projections.OnException<ArithmeticException>()
                .RetryLater(50.Milliseconds(), 50.Milliseconds());
        });

        using var node = await StartDaemon();

        NumberOfStreams = 10;
        await PublishMultiThreaded(3);

        await node.WaitForShardToStop("one:All");
        node.StatusFor("one:All").ShouldBe(AgentStatus.Stopped);
    }


    [Fact]
    public async Task projections_are_stopped_with_unhandled_exceptions()
    {
        var projection1 = new SometimesFailingProjection("one");
        projection1.StartThrowingExceptionsAtSequence(10, new ArithmeticException(), new BadImageFormatException());

        var projection2 = new SometimesFailingProjection("two");

        StoreOptions(opts =>
        {
            opts.Projections.Add(projection1, ProjectionLifecycle.Async);
            opts.Projections.Add(projection2, ProjectionLifecycle.Async);
            opts.Projections.OnException<ArithmeticException>()
                .RetryLater(50.Milliseconds(), 50.Milliseconds());
        });

        using var node = await StartDaemon();

        NumberOfStreams = 10;
        await PublishSingleThreaded();
        await node.Tracker.WaitForHighWaterMark(NumberOfEvents);

        await node.WaitForShardToStop("one:All");

        node.StatusFor("one:All").ShouldBe(AgentStatus.Stopped);
        node.StatusFor("two:All").ShouldBe(AgentStatus.Running);
    }


    [Fact]
    public async Task configured_stop_all()
    {
        var projection1 = new SometimesFailingProjection("one");
        projection1.StartThrowingExceptionsAtSequence(10, new DivideByZeroException());

        var projection2 = new SometimesFailingProjection("two");

        StoreOptions(opts =>
        {
            opts.Projections.Add(projection1, ProjectionLifecycle.Async);
            opts.Projections.Add(projection2, ProjectionLifecycle.Async);
            opts.Projections.OnException<ApplyEventException>().AndInner<DivideByZeroException>().StopAll();
        });

        using var node = await StartDaemon();

        NumberOfStreams = 10;
        await PublishSingleThreaded();
        await node.Tracker.WaitForHighWaterMark(NumberOfEvents);

        await node.WaitForShardToStop("two:All");
        await node.WaitForShardToStop("one:All");


        node.StatusFor("one:All").ShouldBe(AgentStatus.Stopped);
        node.StatusFor("two:All").ShouldBe(AgentStatus.Stopped);
    }




}

public class SometimesFailingProjection: EventProjection
{
    public SometimesFailingProjection(string projectionName)
    {
        ProjectionName = projectionName;
    }

    private readonly IList<IMaybeThrower> _throwers = new List<IMaybeThrower>();


    internal void FailEveryXTimes(int number, Func<Exception> func)
    {
        _throwers.Add(new SystematicFailure(number, func));
    }

    internal void StartThrowingExceptionsAtSequence(long sequence, params Exception[] exceptions)
    {
        var thrower = new StartThrowingExceptionsAtSequenceThrower(sequence, exceptions);
        _throwers.Add(thrower);
    }



    public class StartThrowingExceptionsAtSequenceThrower: IMaybeThrower
    {
        private readonly long _sequence;
        private readonly Queue<Exception> _exceptions;

        public StartThrowingExceptionsAtSequenceThrower(long sequence, Exception[] exceptions)
        {
            _sequence = sequence;
            _exceptions = new Queue<Exception>(exceptions);
        }

        public void Process(IEvent<Travel> travel)
        {
            if (travel.Sequence >= _sequence && _exceptions.Any())
            {
                throw _exceptions.Dequeue();
            }
        }
    }


    public void Project(IEvent<Travel> e, IDocumentOperations operations)
    {
        foreach (var thrower in _throwers)
        {
            thrower.Process(e);
        }
    }

    public interface IMaybeThrower
    {
        void Process(IEvent<Travel> travel);
    }

    public class SystematicFailure: IMaybeThrower
    {
        private readonly int _number;
        private readonly Func<Exception> _func;

        public SystematicFailure(int number, Func<Exception> func)
        {
            _number = number;
            _func = func;
        }

        public void Process(IEvent<Travel> travel)
        {
            if (travel.Sequence % _number == 0)
            {
                throw _func();
            }
        }
    }
}