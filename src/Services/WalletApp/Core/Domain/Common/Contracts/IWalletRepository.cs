using BuildingBlocks.Contracts;
using BuildingBlocks.Enums;
using WalletApp.Domain.Aggregates.WalletAggregate;

namespace WalletApp.Domain.Common.Contracts;

/// <summary>
/// Repository contract for Wallet aggregate
/// </summary>
public interface IWalletRepository : IRepositoryBase<Wallet, Guid>
{
    #region For retry create wallet   

    /// <summary>
    /// Get user IDs that already have wallets
    /// </summary>
    Task<List<Guid>> GetUserIdsWithWalletAsync(CancellationToken cancellationToken = default);
    #endregion

    /// <summary>
    /// Get active wallets with credits due soon
    /// </summary>
    Task<IEnumerable<Wallet>> GetWalletsWithCreditsDueSoonAsync(
        int daysFromNow = 7,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get wallets with overdue credits
    /// </summary>
    Task<IEnumerable<Wallet>> GetWalletsWithOverdueCreditsAsync(CancellationToken cancellationToken = default);


    /// <summary>
    /// Get total balance for all currency accounts (converted to IRR)
    /// </summary>
    Task<decimal> GetTotalBalanceInIrrAsync(Guid walletId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get daily transaction summary for limits checking
    /// </summary>
    Task<decimal> GetDailyTransactionAmountAsync(
        Guid walletId,
        CurrencyCode currency,
        DateTime date,
        CancellationToken cancellationToken = default);
}