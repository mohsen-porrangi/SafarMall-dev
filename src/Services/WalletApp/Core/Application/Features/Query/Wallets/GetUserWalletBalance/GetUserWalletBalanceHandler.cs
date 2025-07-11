using Microsoft.EntityFrameworkCore;
using WalletApp.Application.Features.Query.Wallets.Shared;
using WalletApp.Domain.Common.Contracts;
using WalletApp.Domain.Exceptions;

namespace WalletApp.Application.Features.Query.Wallets.GetUserWalletBalance;

public class GetUserWalletBalanceHandler(IUnitOfWork unitOfWork)
    : IQueryHandler<GetUserWalletBalanceQuery, UserWalletBalanceDto>
{
    public async Task<UserWalletBalanceDto> Handle(GetUserWalletBalanceQuery request, CancellationToken cancellationToken)
    {
        // Get wallet with filtered currency accounts using optimized query
        var wallet = await unitOfWork.Wallets
            .FirstOrDefaultWithIncludesAsync(
                w => w.UserId == request.UserId && !w.IsDeleted,
                q => q.Include(w => w.CurrencyAccounts.Where(ca => ca.IsActive && !ca.IsDeleted)),
                cancellationToken: cancellationToken);

        if (wallet == null)
            throw new WalletNotFoundException(request.UserId);

        // Get transaction stats using optimized query
        var transactionStats = await unitOfWork.Transactions
            .GetTransactionStatisticsAsync(request.UserId, cancellationToken: cancellationToken);

        // Build currency balances (already filtered in database)
        var currencyBalances = wallet.CurrencyAccounts
            .Select(a => new CurrencyBalanceDto
            {
                Currency = a.Currency,
                Balance = a.Balance.Value,
                IsActive = a.IsActive
            });

        var totalBalanceInIrr = await unitOfWork.Wallets
            .GetTotalBalanceInIrrAsync(wallet.Id, cancellationToken);

        return new UserWalletBalanceDto
        {
            WalletId = wallet.Id,
            UserId = wallet.UserId,
            IsActive = wallet.IsActive,
            TotalBalanceInIrr = totalBalanceInIrr,
            CurrencyBalances = currencyBalances,
         // TODO  LastTransactionDate = transactionStats.LastTransactionDate,
            TotalTransactionsCount = transactionStats.TotalTransactions
        };
    }
}