using BuildingBlocks.Contracts;
using BuildingBlocks.Domain;
using BuildingBlocks.Enums;
using BuildingBlocks.Exceptions;
using BuildingBlocks.ValueObjects;
using WalletApp.Domain.Common;
using WalletApp.Domain.Enums;
using WalletApp.Domain.Events;
using WalletApp.Domain.Exceptions;

namespace WalletApp.Domain.Aggregates.WalletAggregate;

/// <summary>
/// Wallet Aggregate Root - Main wallet entity managing all user's financial accounts
/// </summary>
public class Wallet : EntityWithDomainEvents<Guid>, IAggregateRoot, ISoftDelete
{
    private readonly List<CurrencyAccount> _currencyAccounts = new();
    private readonly List<BankAccount> _bankAccounts = new();
    private readonly List<Credit> _credits = new();

    public Guid UserId { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Collections - Expose as readonly
    public virtual IReadOnlyCollection<CurrencyAccount> CurrencyAccounts => _currencyAccounts.AsReadOnly();
    public virtual IReadOnlyCollection<BankAccount> BankAccounts => _bankAccounts.AsReadOnly();
    public virtual IReadOnlyCollection<Credit> Credits => _credits.AsReadOnly();

    // Private constructor for EF Core
    private Wallet() { }

    /// <summary>
    /// Create new wallet for user
    /// </summary>
    public Wallet(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));

        Id = Guid.NewGuid();
        UserId = userId;
        IsActive = true;        

        AddDomainEvent(new WalletCreatedEvent(Id, userId));
    }

    /// <summary>
    /// Create or get currency account
    /// </summary>
    public CurrencyAccount CreateCurrencyAccount(CurrencyCode currency)
    {
        EnsureActive();

        var existingAccount = GetCurrencyAccount(currency);
        if (existingAccount != null)
            return existingAccount;

        ValidateCurrencyAccountCreation(currency);

        var account = new CurrencyAccount(Id, currency);
        _currencyAccounts.Add(account);

        AddDomainEvent(new CurrencyAccountCreatedEvent(Id, account.Id, currency));
        return account;
    }

    /// <summary>
    /// Add bank account
    /// </summary>
    public BankAccount AddBankAccount(
        string bankName,
        string? accountNumber = null,
        string? cardNumber = null,
        string? shabaNumber = null,
        string? accountHolderName = null)
    {
        EnsureActive();
        ValidateBankAccountCreation(accountNumber);

        var bankAccount = new BankAccount(Id, bankName, accountNumber, cardNumber, shabaNumber, accountHolderName);
        _bankAccounts.Add(bankAccount);

        // Set as default if it's the first active bank account
        if (GetActiveBankAccountsCount() == 1)
            bankAccount.SetAsDefault();

        AddDomainEvent(new BankAccountAddedEvent(Id, bankAccount.Id, bankName, accountNumber));
        return bankAccount;
    }

    /// <summary>
    /// Remove bank account
    /// </summary>
    public void RemoveBankAccount(Guid bankAccountId)
    {
        var bankAccount = GetBankAccountById(bankAccountId);
        var wasDefault = bankAccount.IsDefault;

        // حذف منطقی توسط ChangeTrackerExtensions انجام می‌شود
        _bankAccounts.Remove(bankAccount);

        // If removed account was default, set another as default
        if (wasDefault)
        {
            var nextDefault = _bankAccounts.FirstOrDefault(b => b.IsActive);
            nextDefault?.SetAsDefault();
        }
    }

    /// <summary>
    /// Get currency account by currency code
    /// </summary>
    public CurrencyAccount? GetCurrencyAccount(CurrencyCode currency) =>
        _currencyAccounts.FirstOrDefault(a => a.Currency == currency && !a.IsDeleted);

    /// <summary>
    /// Get default currency account (IRR)
    /// </summary>
    public CurrencyAccount? GetDefaultAccount() =>
        GetCurrencyAccount(CurrencyCode.IRR);

    /// <summary>
    /// Get active credit (B2B)
    /// </summary>
    public Credit? GetActiveCredit() =>
        _credits.FirstOrDefault(c => c.Status == CreditStatus.Active && !c.IsDeleted);

    /// <summary>
    /// Check if wallet has sufficient balance
    /// </summary>
    public bool HasSufficientBalance(decimal amount, CurrencyCode currency) =>
        GetCurrencyAccount(currency)?.HasSufficientBalance(amount) ?? false;

    /// <summary>
    /// Get total balance in IRR
    /// </summary>
    public decimal GetTotalBalanceInIrr() =>
        _currencyAccounts
            .Where(a => a.IsActive && !a.IsDeleted && a.Currency == CurrencyCode.IRR)
            .Sum(a => a.Balance.Value);

    /// <summary>
    /// Activate/Deactivate wallet
    /// </summary>
    public void SetActiveStatus(bool isActive)
    {
        IsActive = isActive;

        if (!isActive)
        {
            // Deactivate all accounts when wallet is deactivated
            foreach (var account in _currencyAccounts.Where(a => !a.IsDeleted))
                account.Deactivate();

            foreach (var bankAccount in _bankAccounts.Where(b => !b.IsDeleted))
                bankAccount.Deactivate();
        }
    }

    #region B2B Credit Methods

    /// <summary>
    /// Assign credit limit (B2B)
    /// </summary>
    public void AssignCredit(decimal amount, DateTime dueDate, string description)
    {
        EnsureActive();

        if (GetActiveCredit() != null)
            throw new InvalidOperationException("Active credit already exists");

        var creditMoney = Money.Create(amount, CurrencyCode.IRR);
        ValidateCreditAssignment(creditMoney, dueDate);

        var credit = new Credit(Id, creditMoney, dueDate, description);
        _credits.Add(credit);

        AddDomainEvent(new CreditAssignedEvent(Id, UserId, amount, dueDate));
    }

    #endregion

    #region Private Methods

    private void EnsureActive()
    {
        if (!IsActive)
            throw new InvalidOperationException("Wallet is not active");

        if (IsDeleted)
            throw new InvalidOperationException("Wallet has been deleted");
    }

    private void ValidateCurrencyAccountCreation(CurrencyCode currency)
    {
        if (!BusinessRules.Wallet.CanAddCurrencyAccount(GetActiveCurrencyAccountsCount()))
            throw new InvalidOperationException($"Cannot exceed maximum of {BusinessRules.Wallet.MaxCurrencyAccountsPerWallet} currency accounts");

        if (!BusinessRules.Currency.IsSupportedCurrency(currency))
            throw new InvalidCurrencyException(currency.ToString());
    }

    private void ValidateBankAccountCreation(string? accountNumber)
    {
        if (!BusinessRules.Wallet.CanAddBankAccount(GetActiveBankAccountsCount()))
            throw new InvalidOperationException($"Cannot exceed maximum of {BusinessRules.Wallet.MaxBankAccountsPerWallet} bank accounts");

        if (!string.IsNullOrWhiteSpace(accountNumber) &&
            _bankAccounts.Any(b => b.AccountNumber == accountNumber && !b.IsDeleted))
            throw new InvalidBankAccountException($"Bank account with number {accountNumber} already exists");
    }

    private void ValidateCreditAssignment(Money creditMoney, DateTime dueDate)
    {
        if (!BusinessRules.Credit.IsValidCreditAmount(creditMoney))
            throw new ArgumentException("Invalid credit amount");

        if (!BusinessRules.Credit.IsValidCreditDueDate(dueDate))
            throw new ArgumentException("Invalid credit due date");
    }

    private BankAccount GetBankAccountById(Guid bankAccountId)
    {
        var bankAccount = _bankAccounts.FirstOrDefault(b => b.Id == bankAccountId && !b.IsDeleted);
        if (bankAccount == null)
            throw new NotFoundException("Bank account not found", bankAccountId);
        return bankAccount;
    }

    private int GetActiveCurrencyAccountsCount() =>
        _currencyAccounts.Count(a => !a.IsDeleted);

    private int GetActiveBankAccountsCount() =>
        _bankAccounts.Count(b => !b.IsDeleted);

    #endregion
}