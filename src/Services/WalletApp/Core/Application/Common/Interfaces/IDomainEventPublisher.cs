using BuildingBlocks.MessagingEvent.Base;

namespace WalletApp.Application.Common.Interfaces;

/// <summary>
/// Interface for publishing domain events from entities
/// SOLID: Single responsibility - only handles domain event publishing
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>
    /// Publish domain events from aggregate roots
    /// </summary>
    /// <param name="events">Collection of domain events</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishEventsAsync(IEnumerable<IntegrationEvent> events, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish single domain event
    /// </summary>
    /// <param name="domainEvent">Domain event to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishEventAsync(IntegrationEvent domainEvent, CancellationToken cancellationToken = default);
}