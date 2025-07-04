using BuildingBlocks.Contracts;
using WalletApp.Domain.Common.Contracts;
using WalletApp.Domain.Exceptions;

namespace WalletApp.Application.Features.Command.BankAccounts.AddBankAccount;

/// <summary>
/// Add bank account handler
/// </summary>
public class AddBankAccountHandler(IUnitOfWork unitOfWork, ICurrentUserService userService) : ICommandHandler<AddBankAccountCommand, AddBankAccountResult>
{
    

    public async Task<AddBankAccountResult> Handle(AddBankAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId();
        var wallet = await unitOfWork.Wallets.GetByUserIdWithIncludesAsync(
            userId,
            includeCurrencyAccounts: false,
            includeBankAccounts: true,
            cancellationToken: cancellationToken);

        if (wallet == null)
        {
            throw new WalletNotFoundException(userId);
        }

        var bankAccount = wallet.AddBankAccount(
            wallet.Id,
            request.BankName,
            request.AccountNumber,
            request.CardNumber,
            request.ShabaNumber,
            request.AccountHolderName,
            true,
            true
            );

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AddBankAccountResult
        {
            BankAccountId = bankAccount.Id,
            BankName = bankAccount.BankName,
            MaskedAccountNumber = bankAccount.GetMaskedAccountNumber(),
            MaskedCardNumber = bankAccount.GetMaskedCardNumber(),
            ShabaNumber = bankAccount.ShabaNumber,
            AccountHolderName = bankAccount.AccountHolderName,
            IsVerified = bankAccount.IsVerified,
            IsDefault = bankAccount.IsDefault,
            IsActive = bankAccount.IsActive,
            CreatedAt = bankAccount.CreatedAt
        };
    }
}