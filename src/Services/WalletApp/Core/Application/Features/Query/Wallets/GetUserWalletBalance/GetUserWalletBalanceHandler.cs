using BuildingBlocks.Contracts;
using BuildingBlocks.Contracts.Services;
using WalletApp.Application.Features.Query.Wallets.Shared;
using WalletApp.Domain.Common.Contracts;
using WalletApp.Domain.Exceptions;

namespace WalletApp.Application.Features.Query.Wallets.GetUserWalletBalance
{
    public class GetUserWalletBalanceHandler(IUnitOfWork unitOfWork, IUserManagementService userManagement) : IQueryHandler<GetUserWalletBalanceQuery, UserWalletBalanceDto>
    {
        public  Task<UserWalletBalanceDto> Handle(GetUserWalletBalanceQuery request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            //var wallet = await unitOfWork.Wallets.GetByUserIdWithIncludesAsync(
            //request.UserId, includeCurrencyAccounts: true, cancellationToken: cancellationToken);

            //if (wallet == null)
            //    throw new WalletNotFoundException(request.UserId);

            //// Get user info
            //var userInfo = await currentUser.GetUserInfoAsync(request.UserId, cancellationToken);

            //// Get transaction stats
            //var transactionStats = await unitOfWork.Transactions
            //    .GetTransactionStatisticsAsync(request.UserId, cancellationToken: cancellationToken);

            //// Build currency balances
            //var currencyBalances = wallet.CurrencyAccounts
            //    .Where(a => a.IsActive && !a.IsDeleted)
            //    .Select(a => new CurrencyBalanceDto
            //    {
            //        Currency = a.Currency,
            //        Balance = a.Balance.Value,
            //        IsActive = a.IsActive
            //    });

            //var totalBalanceInIrr = await unitOfWork.Wallets
            //    .GetTotalBalanceInIrrAsync(wallet.Id, cancellationToken);

            //return new UserWalletBalanceDto
            //{
            //    WalletId = wallet.Id,
            //    UserId = wallet.UserId,
            //    IsActive = wallet.IsActive,
            //    TotalBalanceInIrr = totalBalanceInIrr,
            //    CurrencyBalances = currencyBalances,
            //    UserInfo = new UserInfoDto
            //    {
            //        UserId = userInfo.Id,
            //        Email = userInfo.Email,
            //        PhoneNumber = userInfo.PhoneNumber,
            //        IsActive = userInfo.IsActive,
            //        CreatedAt = userInfo.CreatedAt
            //    },
            //    LastTransactionDate = transactionStats.LastTransactionDate,
            //    TotalTransactionsCount = transactionStats.TotalTransactions
            //};
        }
    }    
}
