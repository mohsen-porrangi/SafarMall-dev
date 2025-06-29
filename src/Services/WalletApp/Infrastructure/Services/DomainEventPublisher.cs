using BuildingBlocks.Messaging.Contracts;
using BuildingBlocks.MessagingEvent.Base;
using Microsoft.Extensions.Logging;
using WalletApp.Application.Common.Interfaces;

namespace WalletApp.Infrastructure.Services;

/// <summary>
/// Domain event publisher implementation
/// SOLID: Single responsibility - bridges domain events to message bus
/// </summary>
public class DomainEventPublisher : IDomainEventPublisher
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<DomainEventPublisher> _logger;

    public DomainEventPublisher(
        IMessageBus messageBus,
        ILogger<DomainEventPublisher> logger)
    {
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Publish multiple domain events from aggregate roots
    /// </summary>
    public async Task PublishEventsAsync(IEnumerable<IntegrationEvent> events, CancellationToken cancellationToken = default)
    {
        if (events == null)
            return;

        var eventList = events.ToList();
        if (!eventList.Any())
            return;

        _logger.LogDebug("Publishing {Count} domain events", eventList.Count);

        var publishTasks = eventList.Select(async domainEvent =>
        {
            try
            {
                await _messageBus.PublishAsync(domainEvent, cancellationToken);

                _logger.LogDebug(
                    "Domain event published successfully: {EventType} [ID: {EventId}]",
                    domainEvent.EventType, domainEvent.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish domain event: {EventType} [ID: {EventId}]",
                    domainEvent.EventType, domainEvent.Id);

                // YAGNI: Simple error handling - don't break the flow for individual event failures
                // در آینده می‌توان retry mechanism اضافه کرد
            }
        });

        await Task.WhenAll(publishTasks);

        _logger.LogInformation("Completed publishing {Count} domain events", eventList.Count);
    }

    /// <summary>
    /// Publish single domain event
    /// </summary>
    public async Task PublishEventAsync(IntegrationEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent == null)
            return;

        await PublishEventsAsync(new[] { domainEvent }, cancellationToken);
    }
}