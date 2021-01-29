﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.RabbitMq.Tests.Extensions;
using Rebus.Tests.Contracts;
using Rebus.Transport;

#pragma warning disable 1998

namespace Rebus.RabbitMq.Tests
{
    [TestFixture]
    public class TestRabbitExpress : FixtureBase
    {
        BuiltinHandlerActivator _activator;
        IBus _bus;

        protected override void SetUp()
        {
            var queueName = TestConfig.GetName("expressperf");

            Using(new QueueDeleter(queueName));

            _activator = Using(new BuiltinHandlerActivator());
            _bus = Configure.With(_activator)
                .Logging(l => l.ColoredConsole(LogLevel.Info))
                .Transport(t => t.UseRabbitMq(RabbitMqTransportFactory.ConnectionString, queueName))
                .Options(o =>
                {
                    o.SetMaxParallelism(100);
                    o.SetNumberOfWorkers(0);
                } )
                .Start();
        }

        [TestCase(10, true)]
        [TestCase(10, false)]
        [TestCase(10000, true)]
        [TestCase(10000, false)]
        public async Task TestPerformance(int messageCount, bool express)
        {
            var receivedMessages = 0L;
            _activator.Handle<object>(async msg => Interlocked.Increment(ref receivedMessages));

            //_bus.Advanced.Workers.SetNumberOfWorkers(0);

            var stopwatch = Stopwatch.StartNew();

            var messages = Enumerable.Range(0, messageCount).Select(i => express ? (object) new ExpressMessage() : new NormalMessage());

            foreach (var batch in messages.Batch(100))
            {
                using (var scope = new RebusTransactionScope())
                {
                    foreach (var msg in batch)
                    {
                        await _bus.SendLocal(msg);
                    }

                    await scope.CompleteAsync();
                }
            }
            //foreach (var msg in messages)
            //{
            //    await _bus.SendLocal(msg);
            //}
            //await Task.WhenAll(Enumerable.Range(0, messageCount)
            //    .Select(i => express ? (object)new ExpressMessage() : new NormalMessage())
            //    .Batch(100)
            //    .Select(async batch =>
            //    {
            //        using (var scope = new RebusTransactionScope())
            //        {
            //            foreach(var msg in batch) await _bus.SendLocal(msg);

            //            await scope.CompleteAsync();
            //        }
            //    }));

            var elapsedSending = stopwatch.Elapsed;
            stopwatch.Restart();

            var totalSecondsElapsedSending = elapsedSending.TotalSeconds;

            Console.WriteLine("Sent {0} messages in {1:0.0} s - that's {2:0.0} msg/s", messageCount, totalSecondsElapsedSending, messageCount / totalSecondsElapsedSending);

            _bus.Advanced.Workers.SetNumberOfWorkers(5);

            while (Interlocked.Read(ref receivedMessages) < messageCount)
            {
                Thread.Sleep(1000);
                Console.WriteLine($"Got {Interlocked.Read(ref receivedMessages)} messages...");
            }

            var elapsedReceiving = stopwatch.Elapsed;
            var totalSecondsElapsedReceiving = elapsedReceiving.TotalSeconds;

            Console.WriteLine("Received {0} messages in {1:0.0} s - that's {2:0.0} msg/s", messageCount, totalSecondsElapsedReceiving, messageCount / totalSecondsElapsedReceiving);
        }

        [Express]
        class ExpressMessage
        {
        }

        class NormalMessage
        {
        }

        class ExpressAttribute : HeaderAttribute
        {
            public ExpressAttribute() : base(Headers.Express, "")
            {
            }
        }
    }
}