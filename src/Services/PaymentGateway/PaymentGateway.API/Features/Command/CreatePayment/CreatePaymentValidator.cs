using FluentValidation;
using PaymentGateway.API.Common;

namespace PaymentGateway.API.Features.Command.CreatePayment;

/// <summary>
/// اعتبارسنج ایجاد پرداخت
/// </summary>
public class CreatePaymentValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(BusinessRules.Payment.MinimumAmount)
            .WithMessage($"مبلغ باید حداقل {BusinessRules.Payment.MinimumAmount:N0} ریال باشد")
            .LessThanOrEqualTo(BusinessRules.Payment.MaximumAmount)
            .WithMessage($"مبلغ نباید بیش از {BusinessRules.Payment.MaximumAmount:N0} ریال باشد")
            .Must(amount => amount % 1 == 0)
            .WithMessage("مبلغ باید عدد صحیح باشد");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("توضیحات الزامی است")
            .MaximumLength(500)
            .WithMessage("توضیحات نباید بیش از 500 کاراکتر باشد");   

        RuleFor(x => x.PaymentGateway)
            .IsInEnum()
            .WithMessage("نوع درگاه پرداخت نامعتبر است");

        When(x => !string.IsNullOrEmpty(x.OrderId), () =>
        {
            RuleFor(x => x.OrderId)
                .MaximumLength(100)
                .WithMessage("شناسه سفارش نباید بیش از 100 کاراکتر باشد");
        });
    }
}