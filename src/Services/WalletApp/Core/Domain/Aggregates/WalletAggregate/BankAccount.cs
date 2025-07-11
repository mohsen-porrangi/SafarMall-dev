using BuildingBlocks.Contracts;
using BuildingBlocks.Domain;

namespace WalletApp.Domain.Aggregates.WalletAggregate;

/// <summary>
/// Entity حساب بانکی - برای برگشت وجه و انتقال به حساب واقعی
/// </summary>
public class BankAccount : EntityWithDomainEvents<Guid>, ISoftDelete
{
    public Guid WalletId { get; private set; }
    public string BankName { get; private set; } = string.Empty;
    public string? AccountNumber { get; private set; }
    public string? CardNumber { get; private set; }
    public string? ShabaNumber { get; private set; }
    public string? AccountHolderName { get; private set; }
    public bool IsVerified { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation Property
    public Wallet Wallet { get; private set; } = null!;

    // Private constructor for EF Core
    private BankAccount() { }

    /// <summary>
    /// ایجاد حساب بانکی جدید
    /// </summary>
    public BankAccount(
        Guid walletId,
        string bankName,
        string? accountNumber = null,
        string? cardNumber = null,
        string? shabaNumber = null,
        string? accountHolderName = null)
    {        
        if (walletId == Guid.Empty)
            throw new ArgumentException("WalletId cannot be empty", nameof(walletId));

       // Id = Guid.NewGuid();
        WalletId = walletId;
        BankName = bankName;
        AccountNumber = accountNumber;
        CardNumber = cardNumber;
        ShabaNumber = shabaNumber;
        AccountHolderName = accountHolderName;
        IsVerified = false;
        IsDefault = false;
        IsActive = true;
    }

    /// <summary>
    /// تنظیم وضعیت‌ها
    /// </summary>
    public void SetAsDefault() => IsDefault = true;
    public void UnsetAsDefault() => IsDefault = false;
    public void Verify() => IsVerified = true;
    public void Unverify() => IsVerified = false;
    public void Activate() => IsActive = true;
    public void Deactivate()
    {
        IsActive = false;
        IsDefault = false; // غیرفعال نمی‌تونه پیش‌فرض باشه
    }

    /// <summary>
    /// بروزرسانی اطلاعات
    /// </summary>
    public void UpdateInfo(string? bankName = null, string? accountHolderName = null)
    {
        if (!string.IsNullOrWhiteSpace(bankName))
            BankName = bankName;

        if (!string.IsNullOrWhiteSpace(accountHolderName))
            AccountHolderName = accountHolderName;
    }

    /// <summary>
    /// دریافت شماره‌های ماسک شده
    /// </summary>
    public string GetMaskedAccountNumber() =>
        string.IsNullOrEmpty(AccountNumber) || AccountNumber.Length <= 4
            ? AccountNumber ?? string.Empty
            : $"****{AccountNumber[^4..]}";

    public string GetMaskedCardNumber() =>
        string.IsNullOrEmpty(CardNumber) || CardNumber.Length <= 4
            ? CardNumber ?? string.Empty
            : $"**** **** **** {CardNumber[^4..]}";

    /// <summary>
    /// دریافت خلاصه حساب
    /// </summary>
    public string GetSummary()
    {
        var identifier = GetPrimaryIdentifier();
        var status = IsVerified ? "تایید شده" : "تایید نشده";
        var defaultText = IsDefault ? " (پیش‌فرض)" : "";

        return $"{BankName} - {identifier} - {status}{defaultText}";
    }

    private string GetPrimaryIdentifier() =>
        !string.IsNullOrEmpty(AccountNumber) ? GetMaskedAccountNumber() :
        !string.IsNullOrEmpty(CardNumber) ? GetMaskedCardNumber() :
        !string.IsNullOrEmpty(ShabaNumber) ? ShabaNumber :
        "بدون شناسه";
}