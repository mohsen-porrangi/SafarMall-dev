using BuildingBlocks.CQRS;
using Microsoft.EntityFrameworkCore;
using WalletApp.Domain.Common.Contracts;

namespace WalletApp.Application.Features.Command.BankAccounts.RemoveBankAccount;

/// <summary>
/// Remove bank account handler
/// </summary>
public class RemoveBankAccountHandler(IUnitOfWork unitOfWork) : ICommandHandler<RemoveBankAccountCommand, RemoveBankAccountResult>
{    
    public async Task<RemoveBankAccountResult> Handle(RemoveBankAccountCommand request, CancellationToken cancellationToken)
    {
        //var wallet = await unitOfWork.Wallets.GetByUserIdWithIncludesAsync(
        //    request.UserId,
        //    includeCurrencyAccounts: false,
        //    includeBankAccounts: true,
        //    cancellationToken: cancellationToken); // TODO check later

        var wallet = await unitOfWork.Wallets.FirstOrDefaultWithIncludesAsync(
             x => x.UserId.Equals(request.UserId),
             i => i.Include(x => x.BankAccounts));

        if (wallet == null)
        {
            return new RemoveBankAccountResult
            {
                IsSuccessful = false,
                ErrorMessage = "کیف پول یافت نشد",
                BankAccountId = request.BankAccountId
            };
        }

        try
        {
            wallet.RemoveBankAccount(request.BankAccountId);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new RemoveBankAccountResult
            {
                IsSuccessful = true,
                BankAccountId = request.BankAccountId,
                RemovedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new RemoveBankAccountResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                BankAccountId = request.BankAccountId
            };
        }
    }
}