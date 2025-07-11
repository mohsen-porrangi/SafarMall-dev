namespace WalletApp.Application.Features.Query.Wallets.Shared;

public record WalletBalanceDto
{
    public Guid WalletId { get; init; }
    public Guid UserId { get; init; }
    public bool IsActive { get; init; }
    public decimal TotalBalanceInIrr { get; init; }
    public IEnumerable<CurrencyBalanceDto> CurrencyBalances { get; init; } = [];
}

public record CurrencyBalanceDto
{
    public CurrencyCode Currency { get; init; }
    public decimal Balance { get; init; }
    public bool IsActive { get; init; }
}
