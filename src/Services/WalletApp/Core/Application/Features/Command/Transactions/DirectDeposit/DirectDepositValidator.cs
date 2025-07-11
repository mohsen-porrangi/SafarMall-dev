using BuildingBlocks.Enums;
using BuildingBlocks.ValueObjects;
using FluentValidation;
using WalletApp.Domain.Common;
using WalletApp.Domain.Enums;
using WalletApp.Domain.ValueObjects;

namespace WalletApp.Application.Features.Command.Transactions.DirectDeposit;

/// <summary>
/// Direct deposit validator
/// </summary>
public class DirectDepositValidator : AbstractValidator<DirectDepositCommand>
{
    public DirectDepositValidator()
    {
        //RuleFor(x => x.UserId)
        //    .NotEmpty()
        //    .WithMessage("شناسه کاربر الزامی است");

        RuleFor(x => x.Amount)
            .Must(amount => BusinessRules.Amounts.IsValidTransactionAmount(Money.Create(amount, CurrencyCode.IRR)))
            .WithMessage($"مبلغ باید بین {BusinessRules.Amounts.MinimumTransactionAmount.Value:N0} تا {BusinessRules.Amounts.MaximumSingleTransactionAmount.Value:N0} ریال باشد");

        RuleFor(x => x.Currency)
            .Must(BusinessRules.Currency.IsSupportedCurrency)
            .WithMessage("ارز انتخابی پشتیبانی نمی‌شود");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("توضیحات الزامی است")
            .MaximumLength(500)
            .WithMessage("توضیحات نباید بیش از 500 کاراکتر باشد");

    }

    private static bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}