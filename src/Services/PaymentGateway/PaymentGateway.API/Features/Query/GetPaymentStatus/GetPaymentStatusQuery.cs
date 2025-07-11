using BuildingBlocks.CQRS;
using BuildingBlocks.Enums;
using PaymentGateway.API.Models;

namespace PaymentGateway.API.Features.Query.GetPaymentStatus;

/// <summary>
/// کوئری دریافت وضعیت پرداخت
/// </summary>
public record GetPaymentStatusQuery : IQuery<GetPaymentStatusResponse>
{
    /// <summary>
    /// شناسه پرداخت در سیستم
    /// </summary>
    public string PaymentId { get; init; } = string.Empty;

    /// <summary>
    /// شناسه مرجع درگاه (اختیاری)
    /// </summary>
    public string? GatewayReference { get; init; }
}

/// <summary>
/// پاسخ وضعیت پرداخت
/// </summary>
public record GetPaymentStatusResponse
{
    public bool IsSuccessful { get; init; }
    public string PaymentId { get; init; } = string.Empty;
    public PaymentGatewayType GatewayType { get; init; }
    public PaymentStatus Status { get; init; }
    public decimal Amount { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? TransactionId { get; init; }
    public string? TrackingCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? PaidAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsExpired { get; init; }
}