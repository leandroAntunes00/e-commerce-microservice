using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SalesService;
using SalesService.Services;
using Messaging;

namespace SalesService.UnitTests;

public class ProgramTests
{
    [Fact(Skip = "Integration-style host test skipped: avoid RabbitMQ and external side-effects in unit test run")]
    public void CreateApp_RegistersServices_And_HostedService()
    {
        // test intentionally skipped
    }
}
