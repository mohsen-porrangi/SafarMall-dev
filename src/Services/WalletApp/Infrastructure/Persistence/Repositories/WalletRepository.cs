using BuildingBlocks.Data;
using BuildingBlocks.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WalletApp.Domain.Aggregates.WalletAggregate;
using WalletApp.Domain.Common.Contracts;
using WalletApp.Domain.Enums;
using WalletApp.Infrastructure.Persistence.Context;

namespace WalletApp.Infrastructure.Persistence.Repositories;

/// <summary>
/// Wallet repository implementation
/// </summary>
public class WalletRepository(WalletDbContext context, ILogger<WalletRepository> logger)
    : RepositoryBase<Wallet, Guid, WalletDbContext>(context), IWalletRepository
{   

    public async Task<IEnumerable<Wallet>> GetWalletsWithCreditsDueSoonAsync(
        int daysFromNow = 7,
        CancellationToken cancellationToken = default)
    {
        var dueDate = DateTime.UtcNow.AddDays(daysFromNow);

        return await DbSet
            .Include(w => w.Credits)
            .Where(w => w.IsActive && !w.IsDeleted)
            .Where(w => w.Credits.Any(c =>
                c.Status == CreditStatus.Active &&
                c.DueDate <= dueDate &&
                !c.IsDeleted))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Wallet>> GetWalletsWithOverdueCreditsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await DbSet
            .Include(w => w.Credits)
            .Where(w => w.IsActive && !w.IsDeleted)
            .Where(w => w.Credits.Any(c =>
                c.Status == CreditStatus.Active &&
                c.DueDate < now &&
                !c.IsDeleted))
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalBalanceInIrrAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        // For now, only IRR is supported
        // TODO: Add currency conversion when exchange service is ready
        var irrBalance = await Context.CurrencyAccounts
            .Where(ca => ca.WalletId == walletId &&
                        ca.Currency == CurrencyCode.IRR &&
                        ca.IsActive &&
                        !ca.IsDeleted)
            .SumAsync(ca => ca.Balance.Value, cancellationToken);

        return irrBalance;
    }

    public async Task<decimal> GetDailyTransactionAmountAsync(
        Guid walletId,
        CurrencyCode currency,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await Context.Transactions
            .Where(t => t.WalletId == walletId &&
                       t.Amount.Currency == currency &&
                       t.TransactionDate >= startOfDay &&
                       t.TransactionDate < endOfDay &&
                       t.Status == TransactionStatus.Completed &&
                       !t.IsDeleted)
            .SumAsync(t => t.Amount.Value, cancellationToken);
    }


    public async Task<List<Guid>> GetUserIdsWithWalletAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // High-performance query - only select UserId, no includes
            return await Query()
                .Where(w => w.IsActive && !w.IsDeleted)
                .Select(w => w.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user IDs with wallet");
            throw;
        }
    }
}