//using BuildingBlocks.MessagingEvent.Base;
//using BuildingBlocks.Messaging.Handlers;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using Newtonsoft.Json;
//using RabbitMQ.Client;
//using RabbitMQ.Client.Events;
//using System.Reflection;
//using System.Text;

//namespace BuildingBlocks.Messaging.RabbitMQ
//{
//    public class RabbitMQEventConsumerIChannel : BackgroundService
//    {
//        private readonly IServiceProvider _serviceProvider;
//        private readonly ILogger<RabbitMQEventConsumerIChannel> _logger;
//        private readonly RabbitMQOptions _options;
//        private IConnection _connection;
//        private IChannel _channel;
//        private readonly Dictionary<Type, bool> _eventTypeHandlerCache = new();

//        public RabbitMQEventConsumerIChannel(
//            IServiceProvider serviceProvider,
//            IOptions<RabbitMQOptions> options,
//            ILogger<RabbitMQEventConsumerIChannel> logger)
//        {
//            _serviceProvider = serviceProvider;
//            _options = options.Value;
//            _logger = logger;
//        }

//        public override async Task StartAsync(CancellationToken cancellationToken)
//        {
//            await InitializeRabbitMQAsync();
//            await SubscribeToEvents();
//            await base.StartAsync(cancellationToken);
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            _logger.LogInformation("RabbitMQ Event Consumer started for service {ServiceName}", _options.ServiceName);

//            stoppingToken.Register(() =>
//            {
//                _logger.LogInformation("RabbitMQ Event Consumer stopping...");
//                _channel?.CloseAsync().GetAwaiter().GetResult();
//                _connection?.CloseAsync().GetAwaiter().GetResult();
//            });

//            await Task.Delay(Timeout.Infinite, stoppingToken);
//        }

//        private async Task InitializeRabbitMQAsync()
//        {
//            var factory = new ConnectionFactory
//            {
//                HostName = _options.HostName,
//                Port = _options.Port,
//                UserName = _options.UserName,
//                Password = _options.Password,
//                VirtualHost = _options.VirtualHost,
//                AutomaticRecoveryEnabled = true,
//                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
//            };

//            _connection = await factory.CreateConnectionAsync($"{_options.ServiceName}-Consumer");
//            _channel = await _connection.CreateChannelAsync();

//            await _channel.ExchangeDeclareAsync(
//                exchange: _options.ExchangeName,
//                type: ExchangeType.Topic,
//                durable: true,
//                autoDelete: false);
//        }

//        private async Task SubscribeToEvents()
//        {
//            var eventTypes = GetSubscribedEventTypes();

//            foreach (var eventType in eventTypes)
//            {
//                await SubscribeToEventType(eventType);
//            }
//        }

//        private async Task SubscribeToEventType(Type eventType)
//        {
//            var routingKey = GetRoutingKeyFromEventType(eventType);
//            var queueName = $"{_options.ServiceName}.{routingKey}";

//            await _channel.QueueDeclareAsync(
//                queue: queueName,
//                durable: true,
//                exclusive: false,
//                autoDelete: false,
//                arguments: null);

//            await _channel.QueueBindAsync(
//                queue: queueName,
//                exchange: _options.ExchangeName,
//                routingKey: routingKey);

//            var consumer = new AsyncEventingBasicConsumer(_channel);
//            consumer.ReceivedAsync += async (model, eventArgs) =>
//            {
//                await ProcessMessage(eventType, eventArgs);
//            };

//            await _channel.BasicConsumeAsync(
//                queue: queueName,
//                autoAck: false,
//                consumer: consumer);

//            _logger.LogInformation("Subscribed to event {EventType} with routing key {RoutingKey} on queue {QueueName}",
//                eventType.Name, routingKey, queueName);
//        }

//        private async Task ProcessMessage(Type eventType, BasicDeliverEventArgs eventArgs)
//        {
//            try
//            {
//                var body = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
//                var eventData = JsonConvert.DeserializeObject(body, eventType);

//                if (eventData == null)
//                {
//                    _logger.LogWarning("Failed to deserialize event data for {EventType}", eventType.Name);
//                    await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, false);
//                    return;
//                }

//                using var scope = _serviceProvider.CreateScope();
//                var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
//                var handlers = scope.ServiceProvider.GetServices(handlerType);

//                if (!handlers.Any())
//                {
//                    _logger.LogWarning("No handlers found for event {EventType}", eventType.Name);
//                    await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);
//                    return;
//                }

//                foreach (var handler in handlers)
//                {
//                    try
//                    {
//                        var handleMethod = handlerType.GetMethod("HandleAsync");
//                        if (handleMethod != null)
//                        {
//                            var task = (Task)handleMethod.Invoke(handler, new object[] { eventData, CancellationToken.None })!;
//                            await task;

//                            _logger.LogInformation("Successfully processed event {EventType} with handler {HandlerType}",
//                                eventType.Name, handler.GetType().Name);
//                        }
//                    }
//                    catch (Exception handlerEx)
//                    {
//                        _logger.LogError(handlerEx, "Handler {HandlerType} failed for event {EventType}",
//                            handler.GetType().Name, eventType.Name);
//                    }
//                }

//                await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);

//                _logger.LogInformation("Successfully processed event {EventType} [ID: {MessageId}]",
//                    eventType.Name, eventArgs.BasicProperties.MessageId);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error processing event {EventType} [ID: {MessageId}]",
//                    eventType.Name, eventArgs.BasicProperties.MessageId);

//                await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, false);
//            }
//        }

//        /// <summary>
//        /// Performance: Cache handler existence check
//        /// </summary>
//        private IEnumerable<Type> GetSubscribedEventTypes()
//        {
//            // Get all loaded assemblies that might contain events
//            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies()
//                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
//                .ToList();

//            var eventTypes = new List<Type>();

//            foreach (var assembly in allAssemblies)
//            {
//                try
//                {
//                    // Find all event types in assembly
//                    var assemblyEventTypes = assembly.GetTypes()
//                        .Where(t => typeof(IntegrationEvent).IsAssignableFrom(t) &&
//                                   !t.IsAbstract &&
//                                   t != typeof(IntegrationEvent))
//                        .ToList();

//                    // Check each event type for handlers
//                    foreach (var eventType in assemblyEventTypes)
//                    {
//                        if (HasHandlerForEventType(eventType))
//                        {
//                            eventTypes.Add(eventType);
//                            _logger.LogDebug("Found handler for event type: {EventType}", eventType.Name);
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogWarning(ex, "Failed to scan assembly {Assembly}", assembly.GetName().Name);
//                }
//            }

//            _logger.LogInformation("Total subscribed event types found: {Count}", eventTypes.Count);

//            if (eventTypes.Count == 0)
//            {
//                _logger.LogWarning("No event handlers found! Make sure handlers are registered in DI.");

//                // Debug: List all registered services
//                using var scope = _serviceProvider.CreateScope();
//                var handlerInterface = typeof(IIntegrationEventHandler<>);
//                var registeredHandlers = scope.ServiceProvider.GetServices<object>()
//                    .Where(s => s?.GetType().GetInterfaces()
//                        .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface) ?? false)
//                    .ToList();

//                _logger.LogDebug("Registered handlers in DI: {Count}", registeredHandlers.Count);
//            }

//            return eventTypes;
//        }

//        /// <summary>        
//        /// Performance: Use caching to avoid repeated DI lookups
//        /// </summary>
//        private bool HasHandlerForEventType(Type eventType)
//        {
//            // Check cache first
//            if (_eventTypeHandlerCache.TryGetValue(eventType, out var cached))
//            {
//                return cached;
//            }

//            try
//            {
//                using var scope = _serviceProvider.CreateScope();
//                var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
//                var handlers = scope.ServiceProvider.GetServices(handlerType);
//                var hasHandlers = handlers.Any();

//                // Cache result
//                _eventTypeHandlerCache[eventType] = hasHandlers;

//                if (!hasHandlers)
//                {
//                    _logger.LogDebug("No handlers found for {EventType} in DI container", eventType.Name);
//                }

//                return hasHandlers;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogWarning(ex, "Error checking handlers for event type {EventType}", eventType.Name);
//                _eventTypeHandlerCache[eventType] = false;
//                return false;
//            }
//        }

//        private string GetRoutingKeyFromEventType(Type eventType)
//        {
//            var eventName = eventType.Name;
//            if (eventName.EndsWith("Event"))
//                eventName = eventName[..^5];

//            return ConvertToRoutingKey(eventName);
//        }

//        private static string ConvertToRoutingKey(string eventName)
//        {
//            var result = new StringBuilder();

//            for (int i = 0; i < eventName.Length; i++)
//            {
//                if (i > 0 && char.IsUpper(eventName[i]))
//                {
//                    result.Append('.');
//                }
//                result.Append(char.ToLower(eventName[i]));
//            }

//            return result.ToString();
//        }

//        public override void Dispose()
//        {
//            _channel?.CloseAsync().GetAwaiter().GetResult();
//            _channel?.Dispose();
//            _connection?.CloseAsync().GetAwaiter().GetResult();
//            _connection?.Dispose();
//            base.Dispose();
//        }
//    }
//}