﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Marten.Schema;
using Marten.Schema.Sequences;
using Marten.Services;
using Shouldly;
using StructureMap;
using Xunit;

namespace Marten.Testing.Schema.Sequences
{
    public class HiloSequenceTests
    {
        private readonly IContainer _container = Container.For<DevelopmentModeRegistry>();

        private readonly HiloSequence theSequence;
        private IConnectionFactory _connectionFactory;

        public HiloSequenceTests()
        {
            _container.GetInstance<DocumentCleaner>().CompletelyRemoveAll();

            var sql = SchemaBuilder.GetSqlScript(new StoreOptions(), "mt_hilo");

            _connectionFactory = _container.GetInstance<IConnectionFactory>();
            _connectionFactory.RunSql(sql);

            
            theSequence = new HiloSequence(_connectionFactory, "foo", new HiloSettings());
        }

        [Fact]
        public void default_values()
        {
            theSequence.CurrentHi.ShouldBe(-1);
            theSequence.Increment.ShouldBe(1);
            theSequence.MaxLo.ShouldBe(1000);
        }

        [Fact]
        public void should_advance_initial_case()
        {
            theSequence.ShouldAdvanceHi().ShouldBeTrue();
        }

        [Fact]
        public void advance_to_next_hi_from_initial_state()
        {
            theSequence.AdvanceToNextHi();

            theSequence.CurrentLo.ShouldBe(1);
            theSequence.CurrentHi.ShouldBe(0);
        }

        [Fact]
        public void advance_to_next_hi_several_times()
        {
            theSequence.AdvanceToNextHi();

            theSequence.AdvanceToNextHi();
            theSequence.CurrentHi.ShouldBe(1);

            theSequence.AdvanceToNextHi();
            theSequence.CurrentHi.ShouldBe(2);

            theSequence.AdvanceToNextHi();
            theSequence.CurrentHi.ShouldBe(3);
        }

        [Fact]
        public void advance_value_from_initial_state()
        {
            // Gotta do this at least once
            theSequence.AdvanceToNextHi();

            theSequence.AdvanceValue().ShouldBe(1);
            theSequence.AdvanceValue().ShouldBe(2);
            theSequence.AdvanceValue().ShouldBe(3);
            theSequence.AdvanceValue().ShouldBe(4);
            theSequence.AdvanceValue().ShouldBe(5);
        }

        [Fact]
        public void read_from_a_single_thread_from_0_to_5000()
        {
            for (var i = 0; i < 5000; i++)
            {
                theSequence.NextLong().ShouldBe(i + 1);
            }
        }

        private Task<List<int>> startThread()
        {
            return Task.Factory.StartNew(() =>
            {
                var list = new List<int>();

                for (int i = 0; i < 1000; i++)
                {
                    list.Add(theSequence.NextInt());
                }

                return list;
            });
        }


        [Fact]
        public void is_thread_safe()
        {
            var tasks = new Task<List<int>>[] {startThread(), startThread(), startThread(), startThread(), startThread(), startThread()};

            Task.WaitAll(tasks);

            var all = tasks.SelectMany(x => x.Result).ToArray();

            all.GroupBy(x => x).Any(x => x.Count() > 1).ShouldBeFalse();

            all.Distinct().Count().ShouldBe(tasks.Length * 1000);

        }

    }
}