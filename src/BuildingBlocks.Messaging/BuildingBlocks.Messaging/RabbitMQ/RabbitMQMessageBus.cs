using BuildingBlocks.Messaging.Contracts;
using BuildingBlocks.MessagingEvent.Base;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace BuildingBlocks.Messaging.RabbitMQ
{
    /// <summary>
    /// RabbitMQ message bus implementation
    /// </summary>
    public class RabbitMQMessageBus : IMessageBus, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly ILogger<RabbitMQMessageBus> _logger;
        private readonly RabbitMQOptions _options;
        private volatile bool _disposed;

        public RabbitMQMessageBus(
            IOptions<RabbitMQOptions> options,
            ILogger<RabbitMQMessageBus> logger)
        {
            _options = options.Value;
            _logger = logger;

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedHeartbeat = TimeSpan.FromSeconds(60),
                ContinuationTimeout = TimeSpan.FromSeconds(20)
            };

            _connection = factory.CreateConnectionAsync($"{_options.ServiceName}-Publisher").GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            // Declare exchange
            _channel.ExchangeDeclareAsync(
                exchange: _options.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false).GetAwaiter().GetResult();

            _logger.LogInformation("RabbitMQ connection established for service {ServiceName}", _options.ServiceName);
        }

        public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : IntegrationEvent
        {
            ThrowIfDisposed();

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            // بررسی وضعیت channel قبل از استفاده
            if (_channel?.IsOpen != true)
            {
                throw new InvalidOperationException("RabbitMQ channel is not available");
            }

            var routingKey = GetRoutingKey<T>();
            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

            var properties = new BasicProperties
            {
                Persistent = true,
                MessageId = message.Id.ToString(),
                CorrelationId = message.CorrelationId ?? Guid.NewGuid().ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                Headers = new Dictionary<string, object?>
                {
                    ["source"] = message.Source,
                    ["event-type"] = typeof(T).Name,
                    ["service"] = _options.ServiceName
                }
            };

            try
            {
                await _channel.BasicPublishAsync(
                    exchange: _options.ExchangeName,
                    routingKey: routingKey,
                    body: body,
                    mandatory: false,
                    basicProperties: properties,
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "Published event {EventType} [ID: {EventId}] [Correlation: {CorrelationId}] to exchange {Exchange} with routing key {RoutingKey}",
                    typeof(T).Name, message.Id, properties.CorrelationId, _options.ExchangeName, routingKey);
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogError(ex, "Channel was disposed during publish operation");
                throw new InvalidOperationException("RabbitMQ channel was disposed during publish", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish event {EventType} [ID: {EventId}] to RabbitMQ",
                    typeof(T).Name, message.Id);
                throw;
            }
        }

        public Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : IntegrationEvent
        {
            return PublishAsync(message, cancellationToken);
        }

        private string GetRoutingKey<T>() where T : IntegrationEvent
        {
            var eventName = typeof(T).Name;
            if (eventName.EndsWith("Event"))
                eventName = eventName[..^5];

            return ConvertToRoutingKey(eventName);
        }

        private static string ConvertToRoutingKey(string eventName)
        {
            var result = new StringBuilder();

            for (int i = 0; i < eventName.Length; i++)
            {
                if (i > 0 && char.IsUpper(eventName[i]))
                {
                    result.Append('.');
                }
                result.Append(char.ToLower(eventName[i]));
            }

            return result.ToString();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RabbitMQMessageBus));
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _channel?.Dispose();
                _connection?.Dispose(); // فقط Dispose برای IConnection هم

                _disposed = true;
                _logger.LogInformation("RabbitMQ connection disposed for service {ServiceName}", _options.ServiceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing RabbitMQ connection");
            }
        }
    }

    /// <summary>
    /// RabbitMQ configuration options
    /// </summary>
    public class RabbitMQOptions
    {
        public const string SectionName = "RabbitMQ";

        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
        public string ExchangeName { get; set; } = "integration.events";
        public string ServiceName { get; set; } = "DefaultService";
    }
}