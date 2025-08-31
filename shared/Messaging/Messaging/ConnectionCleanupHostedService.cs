using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Messaging
{
    internal class ConnectionCleanupHostedService : IHostedService
    {
        private readonly IRabbitMqConnectionManager _connectionManager;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger<ConnectionCleanupHostedService> _logger;

        public ConnectionCleanupHostedService(IRabbitMqConnectionManager connectionManager, IHostApplicationLifetime lifetime, ILogger<ConnectionCleanupHostedService> logger)
        {
            _connectionManager = connectionManager;
            _lifetime = lifetime;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Nothing to do at start
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ConnectionCleanupHostedService stopping: disposing RabbitMQ connection manager.");
            try
            {
                // Dispose the connection manager after other hosted services have stopped (StopAsync is called in reverse
                // registration order). Await disposal so the connection is closed deterministically.
                await _connectionManager.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while disposing RabbitMQ connection manager in StopAsync.");
            }
        }
    }
}
