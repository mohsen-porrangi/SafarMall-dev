using FluentValidation;

namespace PaymentGateway.API.Features.Command.VerifyPayment;

/// <summary>
/// اعتبارسنج تایید پرداخت
/// </summary>
public class VerifyPaymentValidator : AbstractValidator<VerifyPaymentCommand>
{
    public VerifyPaymentValidator()
    {
        RuleFor(x => x.GatewayReference)
            .NotEmpty()
            .WithMessage("شناسه مرجع درگاه الزامی است")
            .MaximumLength(100)
            .WithMessage("شناسه مرجع نباید بیش از 100 کاراکتر باشد");

        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("وضعیت پرداخت الزامی است");

        When(x => x.Amount.HasValue, () =>
        {
            RuleFor(x => x.Amount!.Value)
                .GreaterThan(0)
                .WithMessage("مبلغ باید بزرگتر از صفر باشد");
        });

        RuleFor(x => x.GatewayType)
            .IsInEnum()
            .WithMessage("نوع درگاه نامعتبر است");
    }
}