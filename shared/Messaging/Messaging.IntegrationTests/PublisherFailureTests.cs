using Messaging;
using Messaging.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Messaging.IntegrationTests
{
    public class PublisherFailureTests
    {
        [Fact]
        public async Task PublishAsync_WhenConfirmsFalse_ShouldThrow()
        {
            var mockConnection = new Mock<IConnection>();
            var mockModel = new Mock<IModel>();

            mockConnection.Setup(c => c.CreateModel()).Returns(mockModel.Object);

            var mockConnectionManager = new Mock<IRabbitMqConnectionManager>();
            mockConnectionManager.Setup(m => m.GetConnectionAsync()).ReturnsAsync(mockConnection.Object);

            var logger = Mock.Of<ILogger<RabbitMqPublisher>>();
            var settings = Options.Create(new RabbitMqSettings { ExchangeName = "test_exchange" });

            var mockProps = new Mock<IBasicProperties>();
            mockProps.SetupAllProperties();

            mockModel.Setup(m => m.CreateBasicProperties()).Returns(mockProps.Object);
            mockModel.Setup(m => m.ConfirmSelect());
            mockModel.Setup(m => m.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IBasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>()));
            mockModel.Setup(m => m.WaitForConfirms(It.IsAny<TimeSpan>())).Returns(false);

            var publisher = new RabbitMqPublisher(mockConnectionManager.Object, logger, settings);

            var evt = new OrderCreatedEvent { OrderId = 1, UserId = 2, TotalAmount = 10m, CreatedAt = DateTime.UtcNow };

            await Assert.ThrowsAsync<Exception>(() => publisher.PublishAsync(evt));
        }

        [Fact]
        public async Task PublishAsync_WhenReturnedByBroker_ShouldThrow()
        {
            var mockConnection = new Mock<IConnection>();
            var mockModel = new Mock<IModel>();

            mockConnection.Setup(c => c.CreateModel()).Returns(mockModel.Object);

            var mockConnectionManager = new Mock<IRabbitMqConnectionManager>();
            mockConnectionManager.Setup(m => m.GetConnectionAsync()).ReturnsAsync(mockConnection.Object);

            var logger = Mock.Of<ILogger<RabbitMqPublisher>>();
            var settings = Options.Create(new RabbitMqSettings { ExchangeName = "test_exchange" });

            var mockProps = new Mock<IBasicProperties>();
            mockProps.SetupAllProperties();

            var returnHandler = null as EventHandler<BasicReturnEventArgs>;
            mockModel.SetupAdd(m => m.BasicReturn += It.IsAny<EventHandler<BasicReturnEventArgs>>())
                .Callback<EventHandler<BasicReturnEventArgs>>(h => returnHandler = h);
            mockModel.Setup(m => m.CreateBasicProperties()).Returns(mockProps.Object);
            mockModel.Setup(m => m.ConfirmSelect());
            mockModel.Setup(m => m.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IBasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>()));

            // Make WaitForConfirms block until we signal it so we can invoke the BasicReturn handler while publisher is waiting
            var confirmsTcs = new TaskCompletionSource<bool>();
            mockModel.Setup(m => m.WaitForConfirms(It.IsAny<TimeSpan>())).Returns(() => confirmsTcs.Task.GetAwaiter().GetResult());

            var publisher = new RabbitMqPublisher(mockConnectionManager.Object, logger, settings);

            var evt = new OrderCreatedEvent { OrderId = 2, UserId = 3, TotalAmount = 5m, CreatedAt = DateTime.UtcNow };

            // Simular retorno do broker
            // Como o publisher adiciona handler antes da BasicPublish, chamamos o handler depois de PublishAsync começar.
            var publishTask = publisher.PublishAsync(evt);

            // Invoke return handler (create instance via reflection for compatibility)
            var arg = (BasicReturnEventArgs)Activator.CreateInstance(typeof(BasicReturnEventArgs), true)!;
            returnHandler?.Invoke(this, arg);

            // Now allow WaitForConfirms to continue
            confirmsTcs.SetResult(true);

            await Assert.ThrowsAsync<Exception>(() => publishTask);
        }
    }
}
