using WalletApp.Application.Features.Query.Wallets.Shared;

namespace WalletApp.Application.Features.Query.Wallets.GetUserWalletBalance
{
    public record GetUserWalletBalanceQuery(Guid UserId) : IQuery<UserWalletBalanceDto>;
}
