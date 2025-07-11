using WalletApp.Application.Features.Query.Wallets.Shared;

namespace WalletApp.Application.Features.Query.Wallets.GetWalletBalance;

/// <summary>
/// Get wallet balance query
/// </summary>
public record GetWalletBalanceQuery : IQuery<WalletBalanceDto>;


