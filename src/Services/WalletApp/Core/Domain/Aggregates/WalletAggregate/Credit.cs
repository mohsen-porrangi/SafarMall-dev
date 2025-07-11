using BuildingBlocks.Contracts;
using BuildingBlocks.Domain;
using BuildingBlocks.ValueObjects;
using WalletApp.Domain.Enums;
using WalletApp.Domain.Exceptions;

namespace WalletApp.Domain.Aggregates.WalletAggregate;

/// <summary>
/// Credit entity for B2B customers - اعتبار برای مشتریان تجاری
/// </summary>
public class Credit : BaseEntity<Guid>, ISoftDelete
{
    public Guid WalletId { get; private set; }
    public Money CreditLimit { get; private set; } = null!;
    public Money UsedCredit { get; private set; } = null!;
    public DateTime GrantedDate { get; private set; }
    public DateTime DueDate { get; private set; }
    public DateTime? SettledDate { get; private set; }
    public CreditStatus Status { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public Guid? SettlementTransactionId { get; private set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation property
    public virtual Wallet Wallet { get; private set; } = null!;

    // Computed property
    public Money AvailableCredit => Money.Create(
        CreditLimit.Value - UsedCredit.Value,
        CreditLimit.Currency);

    // Private constructor for EF Core
    private Credit() { }

    /// <summary>
    /// Create new credit assignment
    /// </summary>
    public Credit(Guid walletId, Money creditLimit, DateTime dueDate, string description)
    {
        if (walletId == Guid.Empty)
            throw new ArgumentException("WalletId cannot be empty", nameof(walletId));

        WalletId = walletId;
        CreditLimit = creditLimit ?? throw new ArgumentNullException(nameof(creditLimit));
        UsedCredit = Money.Zero(creditLimit.Currency);
        GrantedDate = DateTime.UtcNow;
        DueDate = dueDate;
        Status = CreditStatus.Active;
        Description = description;
    }

    /// <summary>
    /// Use part of the credit
    /// </summary>
    public void UseCredit(Money amount)
    {
        if (!CanUseCredit(amount))
            throw new InsufficientBalanceException(WalletId, amount.Value, AvailableCredit.Value);

        UsedCredit = Money.Create(UsedCredit.Value + amount.Value, UsedCredit.Currency);
    }

    /// <summary>
    /// Check if credit can be used
    /// </summary>
    public bool CanUseCredit(Money amount)
    {
        if (amount == null || Status != CreditStatus.Active)
            return false;

        if (DateTime.UtcNow > DueDate)
        {
            MarkAsOverdue();
            return false;
        }

        return amount.Currency == CreditLimit.Currency && AvailableCredit.Value >= amount.Value;
    }

    /// <summary>
    /// Settle the credit
    /// </summary>
    public void Settle(Guid settlementTransactionId)
    {
        if (settlementTransactionId == Guid.Empty)
            throw new ArgumentException("Settlement transaction ID cannot be empty");

        if (Status == CreditStatus.Settled)
            return;

        Status = CreditStatus.Settled;
        SettledDate = DateTime.UtcNow;
        SettlementTransactionId = settlementTransactionId;
    }

    /// <summary>
    /// Mark as overdue
    /// </summary>
    public void MarkAsOverdue()
    {
        if (Status == CreditStatus.Active && DateTime.UtcNow > DueDate)
            Status = CreditStatus.Overdue;
    }

    /// <summary>
    /// Extend due date
    /// </summary>
    public void ExtendDueDate(DateTime newDueDate)
    {
        if (newDueDate <= DueDate)
            throw new ArgumentException("New due date must be later than current due date");

        DueDate = newDueDate;

        if (Status == CreditStatus.Overdue)
            Status = CreditStatus.Active;
    }

    /// <summary>
    /// Get credit summary
    /// </summary>
    public string GetSummary() =>
        $"Credit: {AvailableCredit.Value:N0} / {CreditLimit.Value:N0} {CreditLimit.Currency} - {Status} - Due: {DueDate:yyyy-MM-dd}";
}