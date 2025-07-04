using BuildingBlocks.Exceptions;

namespace WalletApp.Domain.Exceptions;
/// <summary>
/// خطای تراکنش نامعتبر
/// </summary>
public class InvalidTransactionException : BadRequestException
{
    public InvalidTransactionException(string reason)
        : base("تراکنش نامعتبر", reason)
    {
    }

    public InvalidTransactionException(string reason, string details)
        : base("تراکنش نامعتبر", $"{reason} - {details}")
    {
    }
}