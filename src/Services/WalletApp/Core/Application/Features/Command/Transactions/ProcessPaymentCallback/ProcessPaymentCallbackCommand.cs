using BuildingBlocks.CQRS;
using BuildingBlocks.Enums;
using WalletApp.Application.Common.Interfaces;
using WalletApp.Domain.Enums;

namespace WalletApp.Application.Features.Command.Transactions.ProcessPaymentCallback;

/// <summary>
/// Process payment callback command
/// </summary>
public record ProcessPaymentCallbackCommand : ICommand<PaymentCallbackResult>
{
    public string Authority { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal? Amount { get; init; }
    public PaymentGatewayType Gateway { get; init; } = PaymentGatewayType.ZarinPal;
    public string? TransactionId { get; init; }
    public string? TrackingCode { get; init; }
}

/// <summary>
/// Payment callback result
/// </summary>
public record PaymentCallbackResult
{
    public bool IsSuccessful { get; init; }
    public bool IsVerified { get; init; }
    public Guid? TransactionId { get; init; }
    public Guid? WalletId { get; init; }
    public decimal? Amount { get; init; }
    public CurrencyCode? Currency { get; init; }
    public decimal? NewBalance { get; init; }
    public string? ReferenceId { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime? ProcessedAt { get; init; }
}
