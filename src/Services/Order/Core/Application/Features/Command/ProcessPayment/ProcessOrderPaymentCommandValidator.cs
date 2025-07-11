using FluentValidation;

namespace Order.Application.Features.Command.ProcessPayment;

/// <summary>
/// Validator for ProcessOrderPaymentCommand
/// </summary>
public class ProcessOrderPaymentCommandValidator : AbstractValidator<ProcessOrderPaymentCommand>
{
    public ProcessOrderPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("شناسه سفارش الزامی است");

        RuleFor(x => x.PaymentGateway)
            .IsInEnum().WithMessage("نوع درگاه پرداخت معتبر نیست");
    }
}