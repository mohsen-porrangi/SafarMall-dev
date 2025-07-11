using Microsoft.EntityFrameworkCore;
using WalletApp.Domain.Common.Contracts;

namespace WalletApp.Application.Features.Query.Wallets.CheckAffordability
{
    public class CheckAffordabilityHandler(IUnitOfWork unitOfWork)
    : IQueryHandler<CheckAffordabilityQuery, AffordabilityDto>
    {
        public async Task<AffordabilityDto> Handle(CheckAffordabilityQuery request, CancellationToken cancellationToken)
        {
            // Get wallet with currency accounts
            var wallet = await unitOfWork.Wallets
                .FirstOrDefaultWithIncludesAsync(
                    w => w.UserId == request.UserId && !w.IsDeleted,
                    q => q.Include(w => w.CurrencyAccounts.Where(ca => ca.IsActive && !ca.IsDeleted)),
                    cancellationToken: cancellationToken);

            if (wallet == null)
            {
                return new AffordabilityDto
                {
                    CanAfford = false,
                    Reason = "کیف پول یافت نشد",
                    AvailableBalance = 0m,
                    RequiredAmount = request.Amount,
                    Shortfall = request.Amount
                };
            }

            if (!wallet.IsActive)
            {
                return new AffordabilityDto
                {
                    CanAfford = false,
                    Reason = "کیف پول غیرفعال است",
                    AvailableBalance = 0m,
                    RequiredAmount = request.Amount,
                    Shortfall = request.Amount
                };
            }

            var account = wallet.GetCurrencyAccount(request.Currency);
            var availableBalance = account?.Balance.Value ?? 0m;

            var canAfford = availableBalance >= request.Amount;
            var shortfall = canAfford ? 0m : request.Amount - availableBalance;

            return new AffordabilityDto
            {
                CanAfford = canAfford,
                Reason = canAfford ? "موجودی کافی است" : "موجودی کافی نیست",
                AvailableBalance = availableBalance,
                RequiredAmount = request.Amount,
                Shortfall = shortfall
            };
        }
    }
}
