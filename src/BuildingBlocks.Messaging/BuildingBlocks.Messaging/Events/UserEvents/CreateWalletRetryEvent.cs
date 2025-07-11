using BuildingBlocks.MessagingEvent.Base;

namespace BuildingBlocks.Messaging.Events.UserEvents;

/// <summary>
/// Event for retrying wallet creation
/// </summary>
public record CreateWalletRetryEvent : IntegrationEvent
{
    public CreateWalletRetryEvent(Guid userId)
    {
        UserId = userId;
        Source = "WalletReconciliation";
    }

    public Guid UserId { get; init; }
}