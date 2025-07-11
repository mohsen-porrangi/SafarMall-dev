using BuildingBlocks.Contracts;
using WalletApp.Domain.Common.Contracts;
using WalletApp.Domain.Exceptions;

namespace WalletApp.Application.Features.Command.BankAccounts.AddBankAccount;

/// <summary>
/// Add bank account handler
/// </summary>
public class AddBankAccountHandler(IUnitOfWork unitOfWork, ICurrentUserService userService)
    : ICommandHandler<AddBankAccountCommand, AddBankAccountResult>
{
    public async Task<AddBankAccountResult> Handle(AddBankAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId();

        // Get wallet without unnecessary includes (performance optimization)
        var wallet = await unitOfWork.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId && !w.IsDeleted, track: true, cancellationToken);

        if (wallet == null)
        {
            throw new WalletNotFoundException(userId);
        }

        // Domain logic handles business rules
        var bankAccount = wallet.AddBankAccount(
            request.BankName,
            request.AccountNumber,
            request.CardNumber,
            request.ShabaNumber,
            request.AccountHolderName
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