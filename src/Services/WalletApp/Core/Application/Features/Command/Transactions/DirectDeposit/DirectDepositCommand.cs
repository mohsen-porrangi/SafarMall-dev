using WalletApp.Application.Common.Interfaces;

namespace WalletApp.Application.Features.Command.Transactions.DirectDeposit;

/// <summary>
/// Direct deposit command - شارژ مستقیم کیف پول
/// </summary>
public record DirectDepositCommand : ICommand<DirectDepositResult>
{
    //public Guid UserId { get; init; }
    public decimal Amount { get; init; }
    public CurrencyCode Currency { get; init; } = CurrencyCode.IRR;
    public string Description { get; init; } = "شارژ مستقیم کیف پول";        
    public PaymentGatewayType PaymentGateway { get; init; }
}

/// <summary>
/// Direct deposit result
/// </summary>
public record DirectDepositResult
{
    public bool IsSuccessful { get; init; }
    public string? PaymentUrl { get; init; }
    public string? Authority { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? PendingTransactionId { get; init; }
}
