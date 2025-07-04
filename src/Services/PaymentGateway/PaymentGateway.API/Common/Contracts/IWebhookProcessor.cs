using BuildingBlocks.Enums;
using PaymentGateway.API.Features.Command.ProcessWebhook;
using PaymentGateway.API.Models;

namespace PaymentGateway.API.Services;

/// <summary>
/// پردازشگر Webhook ها
/// </summary>
public interface IWebhookProcessor
{
    /// <summary>
    /// پردازش Webhook
    /// </summary>
    Task<ProcessWebhookResponse> ProcessWebhookAsync(
        PaymentGatewayType gatewayType,
        string requestBody,
        IDictionary<string, string> headers,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ثبت لاگ Webhook
    /// </summary>
    Task LogWebhookAsync(WebhookLog webhookLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// پاکسازی لاگ‌های قدیمی
    /// </summary>
    Task CleanupOldLogsAsync(CancellationToken cancellationToken = default);
}

