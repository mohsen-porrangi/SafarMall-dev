using BuildingBlocks.CQRS;
using BuildingBlocks.Enums;

namespace WalletApp.Application.Features.Command.Transactions.ProcessWithdrawal;

/// <summary>
/// Command to process withdrawal (purchase) from wallet
/// </summary>
public record ProcessWithdrawalCommand : ICommand<ProcessWithdrawalResponse>
{
    /// <summary>
    /// User ID who is making the withdrawal
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Amount to withdraw
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
    /// Order context (order ID, booking reference, etc.)
    /// </summary>
    public string? OrderContext { get; init; }

    /// <summary>
    /// External reference for tracking
    /// </summary>
    public string? ExternalReference { get; init; }
}

/// <summary>
/// Response for withdrawal processing
/// </summary>
public record ProcessWithdrawalResponse
{
    public bool IsSuccess { get; init; }
    public Guid? TransactionId { get; init; }
    public string? TransactionNumber { get; init; }
    public decimal? RemainingBalance { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }

    public static ProcessWithdrawalResponse Success(Guid transactionId, string transactionNumber, decimal remainingBalance)
    {
        return new ProcessWithdrawalResponse
        {
            IsSuccess = true,
            TransactionId = transactionId,
            TransactionNumber = transactionNumber,
            RemainingBalance = remainingBalance
        };
    }

    public static ProcessWithdrawalResponse Failure(string errorMessage, string? errorCode = null)
    {
        return new ProcessWithdrawalResponse
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}