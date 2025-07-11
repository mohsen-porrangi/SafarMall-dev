using FluentValidation;
using WalletApp.Application.Common.Validation;

namespace WalletApp.Application.Features.Command.Transactions.IntegratedPurchase;

/// <summary>
/// Integrated purchase validator
/// </summary>
public class IntegratedPurchaseValidator : AbstractValidator<IntegratedPurchaseCommand>
{
    public IntegratedPurchaseValidator()
    {    

        RuleFor(x => x.TotalAmount)
            .ValidateTransactionAmount();

        RuleFor(x => x.Currency)
            .ValidateSupportedCurrency();

        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("شناسه سفارش الزامی است")
            .MaximumLength(50).WithMessage("شناسه سفارش نباید بیش از 50 کاراکتر باشد");

        RuleFor(x => x.Description)
            .ValidateTransactionDescription();
        
    }
}