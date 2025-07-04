using FluentValidation;
using WalletApp.Application.Common.Validation;

namespace WalletApp.Application.Features.Command.Transactions.TransferMoney;

/// <summary>
/// Transfer money validator
/// </summary>
public class TransferMoneyValidator : AbstractValidator<TransferMoneyCommand>
{
    public TransferMoneyValidator()
    {
        RuleFor(x => x.FromUserId)
            .NotEmpty()
            .WithMessage("شناسه کاربر مبدا الزامی است");

        RuleFor(x => x.ToUserId)
            .NotEmpty()
            .WithMessage("شناسه کاربر مقصد الزامی است")
            .NotEqual(x => x.FromUserId)
            .WithMessage("کاربر مقصد باید متفاوت از کاربر مبدا باشد");

        RuleFor(x => x.Amount)
            .ValidateTransactionAmount();

        RuleFor(x => x.Currency)
            .ValidateSupportedCurrency();

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("توضیحات الزامی است")
            .MaximumLength(500)
            .WithMessage("توضیحات نباید بیش از 500 کاراکتر باشد");
    }
}