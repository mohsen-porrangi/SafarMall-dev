using BuildingBlocks.CQRS;
using BuildingBlocks.Enums;
using System.Text.Json.Serialization;

namespace PaymentGateway.API.Features.Command.CreatePayment;

/// <summary>
/// دستور ایجاد پرداخت
/// </summary>
public record CreatePaymentCommand : ICommand<CreatePaymentResponse>
{
    /// <summary>
    /// مبلغ پرداخت (ریال)
    /// </summary>
    public decimal Amount { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CurrencyCode Currency { get; init; } = CurrencyCode.IRR;

    /// <summary>
    /// توضیحات پرداخت
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// شناسه سفارش (اختیاری)
    /// </summary>
    public string? OrderId { get; init; }

    /// <summary>
    /// نوع درگاه پرداخت
    /// </summary>
    public PaymentGatewayType PaymentGateway { get; init; } = PaymentGatewayType.ZarinPal;
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
    public bool IsVerified { get; init; } = false;
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
    public DateTime? ExpiresAt { get; init; }
}