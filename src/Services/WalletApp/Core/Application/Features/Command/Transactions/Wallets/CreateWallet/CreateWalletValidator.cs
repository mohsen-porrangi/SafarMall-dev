using FluentValidation;
using WalletApp.Domain.Common;

namespace WalletApp.Application.Features.Command.Transactions.Wallets.CreateWallet;

/// <summary>
/// Create wallet command validator
/// </summary>
public class CreateWalletValidator : AbstractValidator<CreateWalletCommand>
{
    public CreateWalletValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("شناسه کاربر الزامی است")
            .WithErrorCode(DomainErrors.Wallet.InvalidUser);
    }
}
