using BuildingBlocks.MessagingEvent.Base;

namespace BuildingBlocks.Messaging.Contracts
{
    /// <summary>
    /// Message bus interface for distributed services communication
    /// SOLID: Simple, focused interface for RabbitMQ messaging
    /// </summary>
    public interface IMessageBus : IDisposable
    {
        /// <summary>
        /// Publish an event to all subscribed services
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="message">Event message</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : IntegrationEvent;

        /// <summary>
        /// Send a command to specific service(s)
        /// Currently same as Publish - future enhancement for point-to-point messaging
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="message">Event message</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        Task SendAsync<T>(T message, CancellationToken cancellationToken = default) where T : IntegrationEvent;
    }
}