using BuildingBlocks.Enums;
using PaymentGateway.API.Models;

namespace PaymentGateway.API.Providers;

/// <summary>
/// نتیجه ایجاد پرداخت
/// </summary>
public record CreatePaymentResult
{
    public bool IsSuccessful { get; init; }
    public string? GatewayReference { get; init; }
    public string? CallbackUrl { get; init; }
    public string? PaymentUrl { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
}

/// <summary>
/// نتیجه تایید پرداخت
/// </summary>
public record VerifyPaymentResult
{
    public bool IsSuccessful { get; init; }
    public bool IsVerified { get; init; }
    public string? TransactionId { get; init; }
    public string? TrackingCode { get; init; }
    public decimal? ActualAmount { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
    public DateTime? VerificationDate { get; init; }
}

/// <summary>
/// نتیجه بررسی وضعیت
/// </summary>
public record PaymentStatusResult
{
    public bool IsSuccessful { get; init; }
    public PaymentStatus Status { get; init; }
    public decimal? Amount { get; init; }
    public string? TransactionId { get; init; }
    public string? TrackingCode { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime? CreatedAt { get; init; }
    public DateTime? PaidAt { get; init; }
}

/// <summary>
/// رابط ارائه‌دهنده درگاه پرداخت
/// </summary>
public interface IPaymentProvider
{
    /// <summary>
    /// نوع درگاه
    /// </summary>
    PaymentGatewayType GatewayType { get; }

    /// <summary>
    /// ایجاد درخواست پرداخت
    /// </summary>
    Task<CreatePaymentResult> CreatePaymentAsync(
        decimal amount,
        string description,        
        string? orderId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// تایید پرداخت
    /// </summary>
    Task<VerifyPaymentResult> VerifyPaymentAsync(
        string gatewayReference,
        decimal expectedAmount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// بررسی وضعیت پرداخت
    /// </summary>
    Task<PaymentStatusResult> GetPaymentStatusAsync(
        string gatewayReference,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// پردازش Webhook
    /// </summary>
    Task<bool> ProcessWebhookAsync(
        string requestBody,
        IDictionary<string, string> headers,
        CancellationToken cancellationToken = default);
}