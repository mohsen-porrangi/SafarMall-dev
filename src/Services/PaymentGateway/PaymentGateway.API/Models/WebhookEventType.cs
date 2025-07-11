namespace PaymentGateway.API.Models;

/// <summary>
/// نوع رویداد Webhook
/// </summary>
public enum WebhookEventType
{
    PaymentCreated = 1,
    PaymentCompleted = 2,
    PaymentFailed = 3,
    PaymentCancelled = 4
}
