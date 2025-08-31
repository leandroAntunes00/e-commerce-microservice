using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Messaging
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind RabbitMQ settings from configuration
            services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMQ"));

            // Register the connection manager as a singleton
            services.AddSingleton<IRabbitMqConnectionManager>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<RabbitMqSettings>>();
                var logger = sp.GetRequiredService<ILogger<RabbitMqConnectionManager>>();
                return new RabbitMqConnectionManager(settings, logger);
            });

            // Register the publisher as a singleton
            services.AddSingleton<IMessagePublisher>(sp =>
            {
                var connectionManager = sp.GetRequiredService<IRabbitMqConnectionManager>();
                var settings = sp.GetRequiredService<IOptions<RabbitMqSettings>>();
                var logger = sp.GetRequiredService<ILogger<RabbitMqPublisher>>();
                return new RabbitMqPublisher(connectionManager, logger, settings);
            });

            // Note: shutdown timeout config removed to avoid type resolution issues in test project.

            return services;
        }
    }
}
