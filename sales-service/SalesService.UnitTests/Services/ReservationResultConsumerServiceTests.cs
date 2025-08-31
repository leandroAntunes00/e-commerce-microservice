using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SalesService.Services;
using Messaging;
using Messaging.Events;

namespace SalesService.UnitTests.Services;

public class ReservationResultConsumerServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ResolvesConsumerAndProcessesMessage()
    {
        var consumerMock = new Mock<IMessageConsumer>();
        Func<string, Task>? capturedHandler = null;

        consumerMock.Setup(c => c.StartConsumingAsync(It.IsAny<string>(), It.IsAny<Func<string, Task>>()))
            .Callback<string, Func<string, Task>>((q, h) => capturedHandler = h)
            .Returns(Task.CompletedTask);

        consumerMock.Setup(c => c.StopConsumingAsync()).Returns(Task.CompletedTask);

        var processorMock = new Mock<IReservationResultProcessor>();
        processorMock.Setup(p => p.ProcessAsync(It.IsAny<OrderReservationCompletedEvent>())).Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton<IMessageConsumer>(consumerMock.Object);
        services.AddSingleton<IReservationResultProcessor>(processorMock.Object);
        services.AddSingleton(Options.Create(new RabbitMqSettings { QueuePrefix = "test_" }));
        services.AddLogging();

        var sp = services.BuildServiceProvider();
        var logger = sp.GetRequiredService<ILogger<ReservationResultConsumerService>>();

        var svc = new ReservationResultConsumerService(sp, Options.Create(new RabbitMqSettings { QueuePrefix = "test_" }), logger);

        var cts = new CancellationTokenSource();
        var task = svc.StartAsync(cts.Token);

        // ensure StartConsumingAsync was wired
        consumerMock.Verify(c => c.StartConsumingAsync(It.IsAny<string>(), It.IsAny<Func<string, Task>>()), Times.Once);
        Assert.NotNull(capturedHandler);

        // simulate incoming message
        var evt = new OrderReservationCompletedEvent { OrderId = 1, Success = false, Reason = "no stock" };
        var json = System.Text.Json.JsonSerializer.Serialize(evt);
        await capturedHandler!(json);

        // allow some time
        await Task.Delay(50);

        processorMock.Verify(p => p.ProcessAsync(It.Is<OrderReservationCompletedEvent>(e => e.OrderId == 1 && e.Success == false)), Times.Once);

        // stop service
        cts.Cancel();
        await svc.StopAsync(CancellationToken.None);

        consumerMock.Verify(c => c.StopConsumingAsync(), Times.AtLeastOnce);
    }
}
