
namespace WalletApp.Application.Features.Query.Wallets.Shared
{
    public record UserWalletBalanceDto : WalletBalanceDto
    {
     //   public UserDetailDto UserDetail { get; init; } = null!;
     //   public DateTime? LastTransactionDate { get; init; }
        public int TotalTransactionsCount { get; init; }
    }

}
