using Messaging;
using Messaging.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using System.Text.Json;
using Xunit;

namespace Messaging.IntegrationTests
{
    public class PublisherUnitTests
    {
        [Fact]
        public async Task PublishAsync_ShouldCallBasicPublish_AndConfirm()
        {
            var mockConnection = new Mock<IConnection>();
            var mockModel = new Mock<IModel>();

            // Setup CreateModel to return mocked model
            mockConnection.Setup(c => c.CreateModel()).Returns(mockModel.Object);

            var mockConnectionManager = new Mock<IRabbitMqConnectionManager>();
            mockConnectionManager.Setup(m => m.GetConnectionAsync()).ReturnsAsync(mockConnection.Object);

            var logger = Mock.Of<ILogger<RabbitMqPublisher>>();
            var settings = Options.Create(new RabbitMqSettings { ExchangeName = "test_exchange" });

            // Prepare IBasicProperties mock to avoid null refs
            var mockProps = new Mock<IBasicProperties>();
            mockProps.SetupAllProperties();

            // Capture whether ConfirmSelect and BasicPublish are called
            mockModel.Setup(m => m.CreateBasicProperties()).Returns(mockProps.Object);
            mockModel.Setup(m => m.ConfirmSelect());
            mockModel.Setup(m => m.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IBasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>()));
            mockModel.Setup(m => m.WaitForConfirms(It.IsAny<TimeSpan>())).Returns(true);

            var publisher = new RabbitMqPublisher(mockConnectionManager.Object, logger, Options.Create(settings.Value));

            var evt = new OrderCreatedEvent { OrderId = 1, UserId = 2, TotalAmount = 10m, CreatedAt = DateTime.UtcNow };

            await publisher.PublishAsync(evt);

            mockModel.Verify(m => m.ConfirmSelect(), Times.Once);
            mockModel.Verify(m => m.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), true, It.IsAny<IBasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>()), Times.Once);
            mockModel.Verify(m => m.WaitForConfirms(It.IsAny<TimeSpan>()), Times.Once);
        }
    }
}
