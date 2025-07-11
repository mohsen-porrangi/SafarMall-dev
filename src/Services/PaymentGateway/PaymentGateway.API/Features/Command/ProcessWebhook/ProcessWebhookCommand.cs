using BuildingBlocks.CQRS;
using BuildingBlocks.Enums;
using PaymentGateway.API.Models;

namespace PaymentGateway.API.Features.Command.ProcessWebhook;

/// <summary>
/// دستور پردازش Webhook
/// </summary>
public record ProcessWebhookCommand : ICommand<ProcessWebhookResponse>
{
    /// <summary>
    /// نوع درگاه پرداخت
    /// </summary>
    public PaymentGatewayType GatewayType { get; init; }

    /// <summary>
    /// محتوای درخواست
    /// </summary>
    public string RequestBody { get; init; } = string.Empty;

    /// <summary>
    /// هدرهای HTTP
    /// </summary>
    public IDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// IP فرستنده
    /// </summary>
    public string SourceIp { get; init; } = string.Empty;

    /// <summary>
    /// User Agent
    /// </summary>
    public string UserAgent { get; init; } = string.Empty;
}

/// <summary>
/// پاسخ پردازش Webhook
/// </summary>
public record ProcessWebhookResponse
{
    public bool IsSuccessful { get; init; }
    public string? PaymentId { get; init; }
    public PaymentStatus? NewStatus { get; init; }
    public string? ErrorMessage { get; init; }
    public int StatusCode { get; init; } = 200;
}