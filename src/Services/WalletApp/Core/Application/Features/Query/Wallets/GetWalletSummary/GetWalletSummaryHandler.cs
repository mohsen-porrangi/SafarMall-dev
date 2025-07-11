using BuildingBlocks.CQRS;
using Microsoft.EntityFrameworkCore;
using WalletApp.Domain.Common.Contracts;
using WalletApp.Domain.Exceptions;

namespace WalletApp.Application.Features.Query.Wallets.GetWalletSummary;

/// <summary>
/// Get wallet summary handler
/// </summary>
public class GetWalletSummaryHandler(IUnitOfWork unitOfWork) : IQueryHandler<GetWalletSummaryQuery, WalletSummaryDto>
{
    public async Task<WalletSummaryDto> Handle(GetWalletSummaryQuery request, CancellationToken cancellationToken)
    {
        // Get wallet with filtered includes for better performance
        var wallet = await unitOfWork.Wallets
            .FirstOrDefaultWithIncludesAsync(
                w => w.UserId == request.UserId && !w.IsDeleted,
                q => q.Include(w => w.CurrencyAccounts.Where(ca => ca.IsActive && !ca.IsDeleted))
                      .Include(w => w.BankAccounts.Where(ba => !ba.IsDeleted)),
                cancellationToken: cancellationToken);

        if (wallet == null)
        {
            throw new WalletNotFoundException(request.UserId);
        }

        // Get recent transactions (last 10)
        var (recentTransactions, _) = await unitOfWork.Transactions.GetUserTransactionsAsync(
            request.UserId,
            page: 1,
            pageSize: 10,
            cancellationToken: cancellationToken);

        // Get transaction statistics
        var statistics = await unitOfWork.Transactions.GetTransactionStatisticsAsync(
            request.UserId,
            cancellationToken: cancellationToken);

        // Get current month statistics
        var currentMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var currentMonthStats = await unitOfWork.Transactions.GetTransactionStatisticsAsync(
            request.UserId,
            fromDate: currentMonthStart,
            toDate: DateTime.UtcNow,
            cancellationToken: cancellationToken);

        // Get total balance in IRR
        var totalBalanceInIrr = await unitOfWork.Wallets.GetTotalBalanceInIrrAsync(wallet.Id, cancellationToken);

        return new WalletSummaryDto
        {
            WalletId = wallet.Id,
            UserId = wallet.UserId,
            IsActive = wallet.IsActive,
            TotalBalanceInIrr = totalBalanceInIrr,
            CurrencyBalances = wallet.CurrencyAccounts
                .Select(a => new CurrencyBalanceDto
                {
                    Currency = a.Currency,
                    Balance = a.Balance.Value,
                    IsActive = a.IsActive
                }),
            RecentTransactions = recentTransactions.Select(t => new RecentTransactionDto
            {
                Id = t.Id,
                TransactionNumber = t.TransactionNumber.Value,
                Amount = t.Amount.Value,
                Currency = t.Amount.Currency,
                Direction = t.Direction,
                Type = t.Type,
                Status = t.Status,
                Description = t.Description,
                TransactionDate = t.TransactionDate
            }),
            Statistics = new WalletStatisticsDto
            {
                TotalTransactions = statistics.TotalTransactions,
                SuccessfulTransactions = statistics.SuccessfulTransactions,
                TotalDeposits = statistics.TotalDeposits,
                TotalWithdrawals = statistics.TotalWithdrawals,
                CurrentMonthTransactions = currentMonthStats.TotalTransactions
            },
            BankAccounts = wallet.BankAccounts
                .Select(ba => new BankAccountSummaryDto
                {
                    Id = ba.Id,
                    BankName = ba.BankName,
                    MaskedAccountNumber = ba.GetMaskedAccountNumber(),
                    IsDefault = ba.IsDefault,
                    IsVerified = ba.IsVerified
                })
        };
    }
}