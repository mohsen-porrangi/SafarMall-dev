using FluentValidation;
using WalletApp.Application.Common.Validation;

namespace WalletApp.Application.Features.Command.BankAccounts.AddBankAccount;

public class AddBankAccountValidator : AbstractValidator<AddBankAccountCommand>
{
    public AddBankAccountValidator()
    {
        // نام بانک همیشه اجباری است
        RuleFor(x => x.BankName)
            .ValidateBankName();

        // حداقل یکی از سه فیلد باید وارد شود
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.AccountNumber) ||
                      !string.IsNullOrWhiteSpace(x.ShabaNumber) ||
                      !string.IsNullOrWhiteSpace(x.CardNumber))
            .WithMessage("حداقل یکی از شماره حساب، شماره شبا یا شماره کارت باید وارد شود");

        // اگر شماره حساب وارد شده، باید معتبر باشد
        When(x => !string.IsNullOrWhiteSpace(x.AccountNumber), () =>
        {
            RuleFor(x => x.AccountNumber)
                .ValidateBankAccountNumber();
        });

        // اگر شماره کارت وارد شده، باید معتبر باشد
        When(x => !string.IsNullOrWhiteSpace(x.CardNumber), () =>
        {
            RuleFor(x => x.CardNumber)
                .ValidateIranianCardNumber();
        });

        // اگر شماره شبا وارد شده، باید معتبر باشد
        When(x => !string.IsNullOrWhiteSpace(x.ShabaNumber), () =>
        {
            RuleFor(x => x.ShabaNumber)
                .ValidateIranianShabaNumber();
        });

        // اگر نام صاحب حساب وارد شده، باید معتبر باشد
        When(x => !string.IsNullOrWhiteSpace(x.AccountHolderName), () =>
        {
            RuleFor(x => x.AccountHolderName)
                .MaximumLength(200)
                .WithMessage("نام صاحب حساب نباید بیش از 200 کاراکتر باشد");
        });
    }
}