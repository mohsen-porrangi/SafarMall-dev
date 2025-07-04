using BuildingBlocks.MessagingEvent.Base;

namespace BuildingBlocks.Messaging.Events.OrderEvents;

/// <summary>
/// Event published when order creation fails, requiring compensation
/// </summary>
public record OrderCreationFailedIntegrationEvent : IntegrationEvent
{
    public Guid TrainReservationId { get; init; }
    public Guid UserId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string ErrorDetails { get; init; } = string.Empty;
    public string? ReserveToken { get; init; } // برای compensation
    public DateTime FailedAt { get; init; }

    public OrderCreationFailedIntegrationEvent(
        Guid trainReservationId,
        Guid userId,
        string reason,
        string errorDetails = "",
        string? reserveToken = null)
    {
        TrainReservationId = trainReservationId;
        UserId = userId;
        Reason = reason;
        ErrorDetails = errorDetails;
        ReserveToken = reserveToken;
        FailedAt = DateTime.UtcNow;
        Source = "OrderService";
    }
}