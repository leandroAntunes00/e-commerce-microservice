using System;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Messaging;
using SalesService.Data;

namespace SalesService.UnitTests.TestHelpers;

public static class TestServiceProviderFactory
{
    /// <summary>
    /// Creates a ServiceProvider preconfigured with:
    /// - SalesDbContext using InMemory provider with the provided dbName
    /// - IMapper configured with OrderProfile
    /// - a Mock&lt;IMessagePublisher&gt; registered both as the mock and as the concrete IMessagePublisher
    /// - logging
    /// Returns the built ServiceProvider and the Mock for verification in tests.
    /// </summary>
    public static (IServiceProvider ServiceProvider, Mock<IMessagePublisher> PublisherMock) Create(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<SalesDbContext>(opt => opt.UseInMemoryDatabase(dbName));

        var mapper = TestMapperFactory.CreateMapper();
        services.AddSingleton<IMapper>(mapper);

        var publisherMock = new Mock<IMessagePublisher>();
        services.AddSingleton<IMessagePublisher>(publisherMock.Object);
        services.AddSingleton(publisherMock);

        services.AddLogging();

        var sp = services.BuildServiceProvider();
        return (sp, publisherMock);
    }
}
