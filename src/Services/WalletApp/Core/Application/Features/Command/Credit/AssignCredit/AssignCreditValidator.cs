using FluentValidation;
using WalletApp.Domain.Common;

namespace WalletApp.Application.Features.Command.Credit.AssignCredit;

/// <summary>
/// Validator for assign credit command
/// SOLID: Single responsibility - validation only
/// </summary>
public class AssignCreditValidator : AbstractValidator<AssignCreditCommand>
{
    public AssignCreditValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("شناسه کاربر الزامی است");

        RuleFor(x => x.CreditAmount)
            .GreaterThan(0)
            .WithMessage("مبلغ اعتبار باید مثبت باشد")
            .LessThanOrEqualTo(BusinessRules.Credit.MaximumCreditLimit.Value)
            .WithMessage($"مبلغ اعتبار نباید بیش از {BusinessRules.Credit.MaximumCreditLimit.Value:N0} ریال باشد")
            .Must(amount => amount % 1 == 0)
            .WithMessage("مبلغ اعتبار باید عدد صحیح باشد");

        RuleFor(x => x.Currency)
            .IsInEnum()
            .WithMessage("نوع ارز نامعتبر است")
            .Must(BusinessRules.Currency.IsSupportedCurrency)
            .WithMessage("ارز انتخابی پشتیبانی نمی‌شود");

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("تاریخ سررسید باید در آینده باشد")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(BusinessRules.Credit.MaximumCreditDurationDays))
            .WithMessage($"تاریخ سررسید نباید بیش از {BusinessRules.Credit.MaximumCreditDurationDays} روز در آینده باشد");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("توضیحات الزامی است")
            .MaximumLength(500)
            .WithMessage("توضیحات نباید بیش از 500 کاراکتر باشد");

        When(x => !string.IsNullOrEmpty(x.CompanyName), () =>
        {
            RuleFor(x => x.CompanyName)
                .MaximumLength(200)
                .WithMessage("نام شرکت نباید بیش از 200 کاراکتر باشد");
        });

        When(x => !string.IsNullOrEmpty(x.ReferenceNumber), () =>
        {
            RuleFor(x => x.ReferenceNumber)
                .MaximumLength(100)
                .WithMessage("شماره مرجع نباید بیش از 100 کاراکتر باشد");
        });
    }
}