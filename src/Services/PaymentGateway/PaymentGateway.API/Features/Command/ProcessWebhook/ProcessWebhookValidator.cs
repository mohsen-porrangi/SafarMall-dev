using FluentValidation;
using PaymentGateway.API.Common;

namespace PaymentGateway.API.Features.Command.ProcessWebhook;

/// <summary>
/// اعتبارسنج پردازش Webhook
/// </summary>
public class ProcessWebhookValidator : AbstractValidator<ProcessWebhookCommand>
{
    public ProcessWebhookValidator()
    {
        RuleFor(x => x.GatewayType)
            .IsInEnum()
            .WithMessage("نوع درگاه پرداخت نامعتبر است");

        RuleFor(x => x.RequestBody)
            .NotEmpty()
            .WithMessage("محتوای درخواست الزامی است")
            .MaximumLength(BusinessRules.Webhook.MaxContentSize)
            .WithMessage($"حداکثر اندازه محتوا {BusinessRules.Webhook.MaxContentSize} بایت است");

        RuleFor(x => x.SourceIp)
            .NotEmpty()
            .WithMessage("IP فرستنده الزامی است")
            .Matches(@"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$")
            .WithMessage("فرمت IP نامعتبر است");

        RuleFor(x => x.Headers)
            .NotNull()
            .WithMessage("هدرهای HTTP الزامی است");
    }
}