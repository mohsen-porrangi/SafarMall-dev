using BuildingBlocks.Contracts;
using WalletApp.Application.Features.Query.Wallets.Shared;
using WalletApp.Domain.Common.Contracts;
using WalletApp.Domain.Exceptions;

namespace WalletApp.Application.Features.Query.Wallets.GetWalletBalance;

/// <summary>
/// Get wallet balance handler
/// </summary>
public class GetWalletBalanceHandler(IUnitOfWork unitOfWork, ICurrentUserService userService) : IQueryHandler<GetWalletBalanceQuery, WalletBalanceDto>
{   
    public async Task<WalletBalanceDto> Handle(GetWalletBalanceQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId();
        return await GetWalletBalanceAsync(userId, cancellationToken);
    }
    private async Task<WalletBalanceDto> GetWalletBalanceAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var wallet = await unitOfWork.Wallets.GetByUserIdWithIncludesAsync(
            userId, includeCurrencyAccounts: true, cancellationToken: cancellationToken);

        if (wallet == null)
            throw new WalletNotFoundException(userId);

        var currencyBalances = wallet.CurrencyAccounts
            .Where(a => a.IsActive && !a.IsDeleted)
            .Select(a => new CurrencyBalanceDto
            {
                Currency = a.Currency,
                Balance = a.Balance.Value,
                IsActive = a.IsActive
            });

        var totalBalanceInIrr = await unitOfWork.Wallets
            .GetTotalBalanceInIrrAsync(wallet.Id, cancellationToken);

        return new WalletBalanceDto
        {
            WalletId = wallet.Id,
            UserId = wallet.UserId,
            IsActive = wallet.IsActive,
            TotalBalanceInIrr = totalBalanceInIrr,
            CurrencyBalances = currencyBalances
        };
    }
}
