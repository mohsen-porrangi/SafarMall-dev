using WalletApp.Domain.Common.Contracts;

namespace WalletApp.Application.Features.Query.Wallets.GetWalletStatus
{
    public class GetWalletStatusHandler(IUnitOfWork unitOfWork)
     : IQueryHandler<GetWalletStatusQuery, WalletStatusDto>
    {
        public async Task<WalletStatusDto> Handle(GetWalletStatusQuery request, CancellationToken cancellationToken)
        {
            // Check if wallet exists
            var wallet = await unitOfWork.Wallets
                .FirstOrDefaultAsync(w => w.UserId == request.UserId && !w.IsDeleted, cancellationToken: cancellationToken);

            if (wallet == null)
            {
                return new WalletStatusDto
                {
                    HasWallet = false,
                    IsActive = false,
                    TotalBalanceInIrr = 0m,
                    CanMakePayment = false
                };
            }

            var totalBalance = await unitOfWork.Wallets.GetTotalBalanceInIrrAsync(wallet.Id, cancellationToken);

            return new WalletStatusDto
            {
                HasWallet = true,
                IsActive = wallet.IsActive,
                TotalBalanceInIrr = totalBalance,
                CanMakePayment = wallet.IsActive && totalBalance > 0
            };
        }
    }
}
