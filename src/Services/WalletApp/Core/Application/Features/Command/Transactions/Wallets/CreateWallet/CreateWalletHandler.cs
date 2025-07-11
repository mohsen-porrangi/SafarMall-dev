using BuildingBlocks.CQRS;
using BuildingBlocks.Enums;
using Microsoft.EntityFrameworkCore;
using WalletApp.Domain.Aggregates.WalletAggregate;
using WalletApp.Domain.Common.Contracts;
using WalletApp.Domain.Enums;

namespace WalletApp.Application.Features.Command.Transactions.Wallets.CreateWallet;

public class CreateWalletHandler(IUnitOfWork unitOfWork) : ICommandHandler<CreateWalletCommand, CreateWalletResult>
{
    public async Task<CreateWalletResult> Handle(CreateWalletCommand request, CancellationToken cancellationToken)
    {
        // Check if user already has a wallet using optimized query
        var existingWallet = await unitOfWork.Wallets
            .FirstOrDefaultWithIncludesAsync(
                w => w.UserId == request.UserId && !w.IsDeleted,
                q => q.Include(w => w.CurrencyAccounts),
                cancellationToken: cancellationToken);

        if (existingWallet != null)
        {
            // Return existing wallet info instead of throwing exception
            var existingAccount = existingWallet.GetCurrencyAccount(CurrencyCode.IRR);
            return new CreateWalletResult
            {
                WalletId = existingWallet.Id,
                UserId = existingWallet.UserId,
                DefaultAccountId = existingAccount?.Id,
                DefaultCurrency = CurrencyCode.IRR,
                CreatedAt = existingWallet.CreatedAt
            };
        }

        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // Create new wallet
            var wallet = new Wallet(request.UserId);

            // Create default IRR currency account if requested
            CurrencyAccount? defaultAccount = null;
            if (request.CreateDefaultAccount)
            {
                defaultAccount = wallet.CreateCurrencyAccount(CurrencyCode.IRR);
            }

            // Save wallet (includes currency accounts via navigation properties)
            await unitOfWork.Wallets.AddAsync(wallet, ct);
            await unitOfWork.SaveChangesAsync(ct);

            return new CreateWalletResult
            {
                WalletId = wallet.Id,
                UserId = wallet.UserId,
                DefaultAccountId = defaultAccount?.Id,
                DefaultCurrency = CurrencyCode.IRR,
                CreatedAt = wallet.CreatedAt
            };
        }, cancellationToken);
    }
}