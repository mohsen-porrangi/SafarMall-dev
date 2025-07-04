using BuildingBlocks.CQRS;
using BuildingBlocks.Enums;

namespace WalletApp.Application.Features.Command.Transactions.ProcessDeposit;

/// <summary>
/// Command to process deposit from payment gateway
/// </summary>
public record ProcessDepositCommand : ICommand<ProcessDepositResponse>
{
    /// <summary>
    /// User ID from payment verification
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Gateway reference (Authority) to identify the payment
    /// </summary>
    public string GatewayReference { get; init; } = string.Empty;

    /// <summary>
    /// Deposit amount
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Currency code
    /// </summary>
    public CurrencyCode Currency { get; init; } = CurrencyCode.IRR;

    /// <summary>
    /// Transaction description
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Payment reference ID from gateway
    /// </summary>
    public string? PaymentReferenceId { get; init; }

    /// <summary>
    /// Order context if linked to purchase
    /// </summary>
    public string? OrderContext { get; init; }
}

/// <summary>
/// Response for deposit processing
/// </summary>
public record ProcessDepositResponse
{
    public bool IsSuccess { get; init; }
    public Guid? TransactionId { get; init; }
    public string? ErrorMessage { get; init; }
    public decimal? NewBalance { get; init; }
}