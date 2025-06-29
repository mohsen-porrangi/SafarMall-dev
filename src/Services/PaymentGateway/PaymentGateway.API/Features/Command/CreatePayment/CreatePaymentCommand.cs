using BuildingBlocks.CQRS;
using PaymentGateway.API.Models;

namespace PaymentGateway.API.Features.Command.CreatePayment;

/// <summary>
/// دستور ایجاد پرداخت
/// </summary>
public record CreatePaymentCommand : ICommand<CreatePaymentResponse>
{
    /// <summary>
    /// شناسه کاربر - اضافه شده
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// مبلغ پرداخت (ریال)
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// توضیحات پرداخت
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// آدرس بازگشت
    /// </summary>
    public string CallbackUrl { get; init; } = string.Empty;

    /// <summary>
    /// شناسه سفارش (اختیاری)
    /// </summary>
    public string? OrderId { get; init; }

    /// <summary>
    /// نوع درگاه پرداخت
    /// </summary>
    public PaymentGatewayType GatewayType { get; init; } = PaymentGatewayType.ZarinPal;
}

/// <summary>
/// پاسخ ایجاد پرداخت
/// </summary>
public record CreatePaymentResponse
{
    public bool IsSuccessful { get; init; }
    public string? PaymentId { get; init; }
    public string? GatewayReference { get; init; }
    public string? PaymentUrl { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
    public DateTime? ExpiresAt { get; init; }
}