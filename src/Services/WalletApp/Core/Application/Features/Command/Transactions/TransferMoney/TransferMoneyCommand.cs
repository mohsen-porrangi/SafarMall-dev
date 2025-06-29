namespace WalletApp.Application.Features.Command.Transactions.TransferMoney;

/// <summary>
/// Transfer money command
/// </summary>
public record TransferMoneyCommand : ICommand<TransferMoneyResult>
{
    public Guid FromUserId { get; init; }
    public Guid ToUserId { get; init; }
    public decimal Amount { get; init; }
    public CurrencyCode Currency { get; init; } = CurrencyCode.IRR;
    public string Description { get; init; } = string.Empty;
    public string? Reference { get; init; }
}

/// <summary>
/// Transfer money result
/// </summary>
public record TransferMoneyResult
{
    public bool IsSuccessful { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? FromTransactionId { get; init; }
    public Guid? ToTransactionId { get; init; }
    public decimal TransferAmount { get; init; }
    public decimal TransferFee { get; init; }
    public decimal FromWalletNewBalance { get; init; }
    public decimal ToWalletNewBalance { get; init; }
    public string? TransferReference { get; init; }
    public DateTime? ProcessedAt { get; init; }
}