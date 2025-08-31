
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Threading;

namespace Messaging
{
    public class RabbitMqConnectionManager : IRabbitMqConnectionManager
    {
        private readonly ConnectionFactory _connectionFactory;
        private readonly ILogger<RabbitMqConnectionManager> _logger;
        private IConnection? _connection;
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        private bool _disposed;

        public RabbitMqConnectionManager(IOptions<RabbitMqSettings> settings, ILogger<RabbitMqConnectionManager> logger)
        {
            _logger = logger;
            var rabbitMqSettings = settings.Value;

            _connectionFactory = new ConnectionFactory
            {
                HostName = rabbitMqSettings.HostName,
                Port = rabbitMqSettings.Port,
                UserName = rabbitMqSettings.UserName,
                Password = rabbitMqSettings.Password,
                VirtualHost = rabbitMqSettings.VirtualHost,
                DispatchConsumersAsync = true // Important for async consumers
            };
        }

        public async Task<IConnection> GetConnectionAsync()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(RabbitMqConnectionManager));
            }

            if (_connection != null && _connection.IsOpen)
            {
                return _connection;
            }

            await _connectionLock.WaitAsync();
            try
            {
                // Double-check locking
                if (_connection != null && _connection.IsOpen)
                {
                    return _connection;
                }

                _logger.LogInformation("Creating new RabbitMQ connection...");
                // Enable automatic recovery and set network recovery interval
                _connectionFactory.AutomaticRecoveryEnabled = true;
                _connectionFactory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);

                // Retry loop for transient failures
                var attempts = 0;
                while (true)
                {
                    try
                    {
                        _connection = _connectionFactory.CreateConnection();
                        _connection.ConnectionShutdown += OnConnectionShutdown;
                        _connection.CallbackException += OnCallbackException;
                        _connection.ConnectionBlocked += OnConnectionBlocked;
                        _logger.LogInformation("RabbitMQ connection created successfully.");
                        break;
                    }
                    catch (Exception ex) when (attempts < 5)
                    {
                        attempts++;
                        _logger.LogWarning(ex, "Failed to create RabbitMQ connection. Retrying in {Seconds}s... (attempt {Attempt})", 2 * attempts, attempts);
                        await Task.Delay(TimeSpan.FromSeconds(2 * attempts));
                        continue;
                    }
                }

                return _connection!;
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        private void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
        {
            _logger.LogWarning("RabbitMQ connection is blocked. Reason: {Reason}", e.Reason);
        }

        private void OnCallbackException(object? sender, CallbackExceptionEventArgs e)
        {
            _logger.LogWarning(e.Exception, "A callback exception occurred in RabbitMQ connection.");
        }

        private void OnConnectionShutdown(object? sender, ShutdownEventArgs reason)
        {
            _logger.LogWarning("RabbitMQ connection was shut down. Reason: {Reason}", reason.ReplyText);
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                if (_connection != null)
                {
                    try
                    {
                        // Try an orderly close first
                        try { _connection.Close(); } catch { }

                        // Ensure any background I/O threads are stopped promptly
                        try { _connection.Abort(); } catch { }

                        // Unsubscribe handlers
                        try
                        {
                            _connection.ConnectionShutdown -= OnConnectionShutdown;
                            _connection.CallbackException -= OnCallbackException;
                            _connection.ConnectionBlocked -= OnConnectionBlocked;
                        }
                        catch { }

                        _connection.Dispose();
                        _connection = null;
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while disposing RabbitMQ connection.");
            }
            _connectionLock?.Dispose();
            await Task.CompletedTask;
        }
    }
}
