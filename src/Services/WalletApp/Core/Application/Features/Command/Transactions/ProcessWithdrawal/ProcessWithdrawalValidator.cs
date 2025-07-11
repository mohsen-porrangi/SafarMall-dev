using FluentValidation;
using WalletApp.Domain.Common;

namespace WalletApp.Application.Features.Command.Transactions.ProcessWithdrawal;

/// <summary>
/// Validator for ProcessWithdrawalCommand
/// SOLID: Single responsibility - only validates withdrawal commands
/// </summary>
public class ProcessWithdrawalValidator : AbstractValidator<ProcessWithdrawalCommand>
{
    public ProcessWithdrawalValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("شناسه کاربر الزامی است");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("مبلغ برداشت باید بزرگتر از صفر باشد")
            .LessThanOrEqualTo(BusinessRules.Transaction.MaxTransactionAmount)
            .WithMessage($"حداکثر مبلغ برداشت {BusinessRules.Transaction.MaxTransactionAmount:N0} ریال است")
            .Must(amount => amount % 1 == 0)
            .WithMessage("مبلغ برداشت باید عدد صحیح باشد");

        RuleFor(x => x.Currency)
            .IsInEnum()
            .WithMessage("نوع ارز نامعتبر است")
            .Must(BusinessRules.Currency.IsSupportedCurrency)
            .WithMessage("ارز انتخابی پشتیبانی نمی‌شود");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("توضیحات تراکنش الزامی است")
            .MaximumLength(500)
            .WithMessage("توضیحات نباید بیش از 500 کاراکتر باشد")
            .MinimumLength(5)
            .WithMessage("توضیحات باید حداقل 5 کاراکتر باشد");

        When(x => !string.IsNullOrEmpty(x.OrderContext), () =>
        {
            RuleFor(x => x.OrderContext)
                .MaximumLength(100)
                .WithMessage("شناسه سفارش نباید بیش از 100 کاراکتر باشد");
        });

        When(x => !string.IsNullOrEmpty(x.ExternalReference), () =>
        {
            RuleFor(x => x.ExternalReference)
                .MaximumLength(200)
                .WithMessage("مرجع خارجی نباید بیش از 200 کاراکتر باشد");
        });
    }
}