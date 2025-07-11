using BuildingBlocks.Exceptions;
namespace WalletApp.Domain.Exceptions;
/// <summary>
/// خطای ارز نامعتبر
/// </summary>
public class InvalidCurrencyException : BadRequestException
{
    public InvalidCurrencyException(string currencyCode)
        : base("ارز نامعتبر", $"ارز {currencyCode} پشتیبانی نمی‌شود")
    {
        CurrencyCode = currencyCode;
    }

    public string CurrencyCode { get; }
}
