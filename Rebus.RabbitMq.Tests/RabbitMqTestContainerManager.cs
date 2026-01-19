using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Testcontainers.RabbitMq;

namespace Rebus.RabbitMq.Tests;

[SetUpFixture]
public class RabbitMqTestContainerManager
{
    static readonly Lazy<RabbitMqContainer> _container = new(() =>
    {
        var container = new RabbitMqBuilder().Build();

        Console.WriteLine("Starting RabbitMQ test container");

        container.StartAsync().GetAwaiter().GetResult();

        return container;
    });

    [OneTimeTearDown]
    public async Task StopRabbitMq()
    {
        if (!_container.IsValueCreated) return;

        var container = _container.Value;

        Console.WriteLine("Stopping RabbitMQ test container");

        await container.StopAsync();
        await container.DisposeAsync();
    }

    public static string GetConnectionString() => "amqp://localhost:5672";
    //public static string GetConnectionString() => _container.Value.GetConnectionString();

    public static CustomContainer GetCustomContainer(Func<RabbitMqBuilder, RabbitMqBuilder> build)
    {
        var rabbitMqBuilder = build(new RabbitMqBuilder());
        var container = rabbitMqBuilder.Build();
        container.StartAsync().GetAwaiter().GetResult();
        return new CustomContainer(container.GetConnectionString(), () =>
        {
            Task.Run(async () =>
                {
                    await container.StopAsync();
                    await container.DisposeAsync();
                })
                .GetAwaiter().GetResult();
        });
    }

    public class CustomContainer : IDisposable
    {
        readonly Action _dispose;

        public string ConnnectionString { get; }

        public CustomContainer(string connnectionString, Action dispose)
        {
            _dispose = dispose;
            ConnnectionString = connnectionString;
        }

        public void Dispose() => _dispose();
    }
}