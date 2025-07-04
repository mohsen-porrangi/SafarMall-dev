using BuildingBlocks.CQRS;
using BuildingBlocks.Enums;
using PaymentGateway.API.Models;

namespace PaymentGateway.API.Features.Command.VerifyPayment;

/// <summary>
/// دستور تایید پرداخت
/// </summary>
public record VerifyPaymentCommand : ICommand<VerifyPaymentResponse>
{
    /// <summary>
    /// شناسه مرجع درگاه (Authority)
    /// </summary>
    public string GatewayReference { get; init; } = string.Empty;

    /// <summary>
    /// وضعیت بازگشتی از درگاه
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// مبلغ (برای اعتبارسنجی)
    /// </summary>
    public decimal? Amount { get; init; }

    /// <summary>
    /// نوع درگاه
    /// </summary>
    public PaymentGatewayType GatewayType { get; init; } = PaymentGatewayType.ZarinPal;
}

/// <summary>
/// پاسخ تایید پرداخت
/// </summary>
public record VerifyPaymentResponse
{
    public bool IsSuccessful { get; init; }
    public bool IsVerified { get; init; }
    public string? PaymentId { get; init; }
    public string? TransactionId { get; init; }
    public string? TrackingCode { get; init; }
    public decimal? Amount { get; init; }
    public PaymentStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
    public DateTime? VerificationDate { get; init; }
}