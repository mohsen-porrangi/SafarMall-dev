﻿using BuildingBlocks.Contracts;
using BuildingBlocks.Domain;
using WalletApp.Domain.Exceptions;

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
        string accountNumber,
        string? cardNumber = null,
        string? shabaNumber = null,
        string? accountHolderName = null)
    {
        if (walletId == Guid.Empty)
            throw new ArgumentException("شناسه کیف پول نمی‌تواند خالی باشد", nameof(walletId));

        //ValidateInputs(bankName, accountNumber, cardNumber, shabaNumber);

        Id = Guid.NewGuid();
        WalletId = walletId;
        BankName = bankName.Trim();
        AccountNumber = accountNumber.Trim();
        CardNumber = cardNumber?.Trim();
        ShabaNumber = shabaNumber?.Trim();
        AccountHolderName = accountHolderName?.Trim();
        IsVerified = false;
        IsDefault = false;
        IsActive = true;

        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// تنظیم به عنوان حساب پیش‌فرض
    /// </summary>
    public void SetAsDefault()
    {
        EnsureActive();
        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// برداشتن از حالت پیش‌فرض
    /// </summary>
    public void UnsetAsDefault()
    {
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// تایید حساب بانکی
    /// </summary>
    public void Verify()
    {
        EnsureActive();
        IsVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// لغو تایید حساب بانکی
    /// </summary>
    public void Unverify()
    {
        IsVerified = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// فعال/غیرفعال کردن حساب
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        IsDefault = false; // اگر غیرفعال شد، پیش‌فرض هم نیست
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// حذف منطقی حساب
    /// </summary>
    public void SoftDelete()
    {
        IsDeleted = true;
        IsActive = false;
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// بروزرسانی اطلاعات حساب
    /// </summary>
    public void UpdateInfo(
        string? bankName = null,
        string? accountHolderName = null)
    {
        EnsureActive();

        if (!string.IsNullOrWhiteSpace(bankName))
        {
            if (bankName.Length > 100)
                throw new InvalidBankAccountException("نام بانک نمی‌تواند بیش از 100 کاراکتر باشد");

            BankName = bankName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(accountHolderName))
        {
            if (accountHolderName.Length > 200)
                throw new InvalidBankAccountException("نام صاحب حساب نمی‌تواند بیش از 200 کاراکتر باشد");

            AccountHolderName = accountHolderName.Trim();
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// دریافت شماره حساب ماسک شده (امنیت)
    /// </summary>
    public string GetMaskedAccountNumber()
    {
        if (AccountNumber.Length <= 4)
            return AccountNumber;

        return "****" + AccountNumber[^4..];
    }

    /// <summary>
    /// دریافت شماره کارت ماسک شده (امنیت)
    /// </summary>
    public string GetMaskedCardNumber()
    {
        if (string.IsNullOrEmpty(CardNumber) || CardNumber.Length <= 4)
            return CardNumber ?? string.Empty;

        return "**** **** **** " + CardNumber[^4..];
    }

    /// <summary>
    /// اعتبارسنجی ورودی‌ها
    /// </summary>


    /// <summary>
    /// اعمال قوانین کسب و کار
    /// </summary>
    private void EnsureActive()
    {
        if (!IsActive)
            throw new InvalidOperationException("حساب بانکی غیرفعال است");

        if (IsDeleted)
            throw new InvalidOperationException("حساب بانکی حذف شده است");
    }

    /// <summary>
    /// دریافت اطلاعات خلاصه حساب
    /// </summary>
    public string GetSummary()
    {
        var status = IsVerified ? "تایید شده" : "تایید نشده";
        var defaultText = IsDefault ? " (پیش‌فرض)" : "";
        return $"{BankName} - {GetMaskedAccountNumber()} - {status}{defaultText}";
    }
}