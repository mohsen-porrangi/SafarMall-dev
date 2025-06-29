using BuildingBlocks.MessagingEvent.Base;

namespace BuildingBlocks.Messaging.Events.PaymentEvents;

/// <summary>
/// Event published when payment is verified successfully by PaymentGateway
/// Triggers wallet charging in WalletApp
/// </summary>
public record PaymentVerifiedEvent : IntegrationEvent
{
    public string PaymentId { get; init; } = string.Empty;
    public string GatewayReference { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public decimal Amount { get; init; }
    public string TransactionId { get; init; } = string.Empty;
    public string? TrackingCode { get; init; }
    public DateTime VerifiedAt { get; init; }
    public string? OrderContext { get; init; }

    public PaymentVerifiedEvent(
        string paymentId,
        string gatewayReference,
        Guid userId,
        decimal amount,
        string transactionId,
        string? trackingCode = null,
        string? orderContext = null)
    {
        PaymentId = paymentId;
        GatewayReference = gatewayReference;
        UserId = userId;
        Amount = amount;
        TransactionId = transactionId;
        TrackingCode = trackingCode;
        OrderContext = orderContext;
        VerifiedAt = DateTime.UtcNow;
        Source = "PaymentGateway";
    }
}