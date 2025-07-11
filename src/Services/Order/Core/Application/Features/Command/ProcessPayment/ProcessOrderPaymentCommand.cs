using BuildingBlocks.CQRS;
using BuildingBlocks.Enums;

namespace Order.Application.Features.Command.ProcessPayment;

/// <summary>
/// Command for processing order payment through Wallet Service
/// </summary>
public record ProcessOrderPaymentCommand : ICommand<ProcessOrderPaymentResult>
{
    public Guid OrderId { get; init; }
    public PaymentGatewayType PaymentGateway { get; init; } = PaymentGatewayType.ZarinPal;
    public bool UseCredit { get; init; } = false;
}

/// <summary>
/// Result of order payment processing
/// </summary>
public record ProcessOrderPaymentResult
{
    public bool IsSuccessful { get; init; }
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public PurchaseType PaymentType { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal WalletBalance { get; init; }
    public decimal RequiredPayment { get; init; }
    public Guid? PurchaseTransactionId { get; init; }
    public Guid? PaymentTransactionId { get; init; }
    public string? PaymentUrl { get; init; }
    public string? Authority { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime? ProcessedAt { get; init; }
}