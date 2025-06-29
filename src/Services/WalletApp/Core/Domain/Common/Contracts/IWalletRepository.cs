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
    /// Get wallet by user ID with all related data
    /// </summary>
    Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get wallet by user ID with specific includes
    /// </summary>
    Task<Wallet?> GetByUserIdWithIncludesAsync(
        Guid userId,
        bool includeCurrencyAccounts = true,
        bool includeBankAccounts = false,
        bool includeCredits = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user already has a wallet
    /// </summary>
    Task<bool> UserHasWalletAsync(Guid userId, CancellationToken cancellationToken = default);

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
    /// Get currency account by ID
    /// </summary>
    Task<CurrencyAccount?> GetCurrencyAccountByIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get currency account by wallet and currency
    /// </summary>
    Task<CurrencyAccount?> GetCurrencyAccountAsync(
        Guid walletId,
        CurrencyCode currency,
        CancellationToken cancellationToken = default);

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