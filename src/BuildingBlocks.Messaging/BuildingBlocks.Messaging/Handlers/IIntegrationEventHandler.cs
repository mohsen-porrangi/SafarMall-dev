using BuildingBlocks.MessagingEvent.Base;
using System.Threading;
using System.Threading.Tasks;

namespace BuildingBlocks.Messaging.Handlers
{
    /// <summary>
    /// Interface for integration event handlers
    /// </summary>
    /// <typeparam name="TEvent">Event type that inherits from IntegrationEvent</typeparam>
    public interface IIntegrationEventHandler<in TEvent> where TEvent : IntegrationEvent
    {
        /// <summary>
        /// Process an integration event
        /// </summary>
        /// <param name="event">The event to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
    }
}