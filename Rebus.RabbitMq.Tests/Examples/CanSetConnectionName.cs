using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;

namespace Rebus.RabbitMq.Tests.Examples;

[TestFixture]
public class CanSetConnectionName : FixtureBase
{
    [Test]
    [Explicit("Can be executed to inspect the connection properties via the management console")]
    public async Task StartBusAndRunIdleForOneMinute()
    {
        const string queueName = "some-queue";
        
        Using(new QueueDeleter(queueName));
        
        const string myCustomConnectionName = "🤠";
        
        Configure.With(Using(new BuiltinHandlerActivator()))
            .Transport(t => t.UseRabbitMq(RabbitMqTestContainerManager.GetConnectionString(), queueName)
                .SetConnectionName(myCustomConnectionName))
            .Start();

        await Task.Delay(TimeSpan.FromMinutes(1));
    }
}