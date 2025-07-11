using BuildingBlocks.Messaging.Contracts;
using BuildingBlocks.Messaging.Handlers;
using BuildingBlocks.Messaging.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BuildingBlocks.Messaging.Extensions
{    
    public static class MessagingServiceCollectionExtensions
    {
        /// <summary>
        /// Add RabbitMQ messaging infrastructure with proper handler registration
        /// KISS: Simple, one-line registration for all services
        /// </summary>
        public static IServiceCollection AddMessaging(
            this IServiceCollection services,
            IConfiguration configuration,
            params Assembly[] handlersAssemblies)
        {
            // Configure RabbitMQ options from appsettings
            services.Configure<RabbitMQOptions>(
                configuration.GetSection(RabbitMQOptions.SectionName));

            // Register core RabbitMQ services
            services.AddSingleton<IMessageBus, RabbitMQMessageBus>();
            services.AddHostedService<RabbitMQEventConsumer>();
            
            if (handlersAssemblies?.Length > 0)
            {
                RegisterEventHandlers(services, handlersAssemblies);
            }

            return services;
        }

        /// <summary>
        /// Enhanced event handler registration with proper DI setup
        /// SOLID: Single responsibility - register handlers properly
        /// DRY: Reusable for multiple assemblies
        /// </summary>
        private static void RegisterEventHandlers(IServiceCollection services, Assembly[] assemblies)
        {
            var handlerInterface = typeof(IIntegrationEventHandler<>);
            var registeredHandlers = new List<string>();

            foreach (var assembly in assemblies)
            {
                var handlerTypes = assembly.GetTypes()
                    .Where(t => !t.IsAbstract &&
                               !t.IsInterface &&
                               t.GetInterfaces().Any(i =>
                                   i.IsGenericType &&
                                   i.GetGenericTypeDefinition() == handlerInterface))
                    .ToList();

                foreach (var handlerType in handlerTypes)
                {
                    // Find all implemented handler interfaces
                    var implementedHandlerInterfaces = handlerType.GetInterfaces()
                        .Where(i => i.IsGenericType &&
                                   i.GetGenericTypeDefinition() == handlerInterface);

                    foreach (var handlerInterfaceType in implementedHandlerInterfaces)
                    {                        
                        services.AddScoped(handlerInterfaceType, handlerType);
                        services.AddScoped(handlerType);

                        var eventType = handlerInterfaceType.GetGenericArguments()[0];
                        registeredHandlers.Add($"{handlerType.Name} -> {eventType.Name}");
                    }
                }
            }

            // Log registered handlers for debugging
            if (registeredHandlers.Any())
            {
                Console.WriteLine($"[Messaging] Registered {registeredHandlers.Count} event handlers:");
                registeredHandlers.ForEach(h => Console.WriteLine($"  - {h}"));
            }
        }
    }
}