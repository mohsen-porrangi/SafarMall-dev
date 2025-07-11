using BuildingBlocks.Contracts;
using BuildingBlocks.Domain;
using BuildingBlocks.Enums;
using BuildingBlocks.ValueObjects;
using WalletApp.Domain.Aggregates.TransactionAggregate;
using WalletApp.Domain.Enums;
using WalletApp.Domain.Events;
using WalletApp.Domain.Exceptions;

namespace WalletApp.Domain.Aggregates.WalletAggregate;

/// <summary>
/// Currency Account Entity - Handles balance management for specific currency
/// </summary>
public class CurrencyAccount : EntityWithDomainEvents<Guid>, ISoftDelete
{
    private Money _balance = null!;
    private readonly List<Transaction> _transactions = new();

    public Guid WalletId { get; private set; }
    public CurrencyCode Currency { get; private set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }



    public Money Balance
    {
        get => _balance;
        private set => _balance = value ?? throw new ArgumentNullException(nameof(value));
    }
    public bool IsActive { get; private set; }

    // Navigation properties
    public virtual Wallet Wallet { get; private set; } = null!;
    public virtual IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    // Private constructor for EF Core
    private CurrencyAccount() { }

    /// <summary>
    /// Create new currency account
    /// </summary>
    public CurrencyAccount(Guid walletId, CurrencyCode currency)
    {
        if (walletId == Guid.Empty)
            throw new ArgumentException("WalletId cannot be empty", nameof(walletId));

        WalletId = walletId;
        Currency = currency;
        Balance = Money.Zero(currency);
        IsActive = true;
    }

    /// <summary>
    /// Create transaction (generic method to reduce duplication)
    /// </summary>
    private Transaction CreateTransaction(
        Money amount,
        TransactionDirection direction,
        TransactionType type,
        string description,
        string? reference = null,
        Guid? relatedId = null)
    {
        EnsureActive();
        ValidateAmount(amount);

        if (direction == TransactionDirection.Out && !HasSufficientBalance(amount.Value))
            throw new InsufficientBalanceException(WalletId, amount.Value, Balance.Value);

        return type switch
        {
            TransactionType.Deposit => Transaction.CreateDepositTransaction(
                WalletId, Id, Wallet?.UserId ?? Guid.Empty, amount, description, reference),

            TransactionType.Purchase => Transaction.CreatePurchaseTransaction(
                WalletId, Id, Wallet?.UserId ?? Guid.Empty, amount, description, reference),

            TransactionType.Refund => Transaction.CreateRefundTransaction(
                WalletId, Id, Wallet?.UserId ?? Guid.Empty, amount, description, relatedId ?? Guid.Empty),

            TransactionType.Transfer when direction == TransactionDirection.Out =>
                Transaction.CreateTransferOutTransaction(
                    WalletId, Id, Wallet?.UserId ?? Guid.Empty, amount, description, reference ?? string.Empty),

            TransactionType.Transfer when direction == TransactionDirection.In =>
                Transaction.CreateTransferInTransaction(
                    WalletId, Id, Wallet?.UserId ?? Guid.Empty, amount, description, reference ?? string.Empty),

            _ => throw new ArgumentException($"Unsupported transaction type: {type}")
        };
    }

    /// <summary>
    /// Create deposit transaction
    /// </summary>
    public Transaction CreateDepositTransaction(Money amount, string description, string? paymentReferenceId = null) =>
        CreateTransaction(amount, TransactionDirection.In, TransactionType.Deposit, description, paymentReferenceId);

    /// <summary>
    /// Create purchase transaction
    /// </summary>
    public Transaction CreatePurchaseTransaction(Money amount, string description, string? orderContext = null) =>
        CreateTransaction(amount, TransactionDirection.Out, TransactionType.Purchase, description, orderContext);

    /// <summary>
    /// Process transaction (generic method to reduce duplication)
    /// </summary>
    public void ProcessTransaction(Transaction transaction)
    {
        if (transaction == null)
            throw new ArgumentNullException(nameof(transaction));

        if (transaction.CurrencyAccountId != Id)
            throw new InvalidOperationException("Transaction does not belong to this account");

        EnsureActive();

        // Update balance based on direction
        Balance = transaction.Direction == TransactionDirection.In
            ? Balance.Add(transaction.Amount)
            : Balance.Subtract(transaction.Amount);

        transaction.MarkAsCompleted();

        // Emit appropriate domain event
        EmitTransactionEvent(transaction);
    }

    /// <summary>
    /// Process deposit
    /// </summary>
    public void ProcessDeposit(Transaction transaction)
    {
        if (transaction.Direction != TransactionDirection.In)
            throw new InvalidOperationException("Only inbound transactions can be processed as deposits");

        ProcessTransaction(transaction);
    }

    /// <summary>
    /// Process purchase
    /// </summary>
    public void ProcessPurchase(Transaction transaction)
    {
        if (transaction.Direction != TransactionDirection.Out)
            throw new InvalidOperationException("Only outbound transactions can be processed as purchases");

        if (!HasSufficientBalance(transaction.Amount.Value))
            throw new InsufficientBalanceException(WalletId, transaction.Amount.Value, Balance.Value);

        ProcessTransaction(transaction);
    }

    /// <summary>
    /// Process refund
    /// </summary>
    public void ProcessRefund(Transaction transaction)
    {
        if (transaction.Type != TransactionType.Refund)
            throw new InvalidOperationException("Transaction must be a refund type");

        ProcessTransaction(transaction);
    }

    /// <summary>
    /// Process transfer
    /// </summary>
    public void ProcessTransfer(Transaction transaction, Money actualAmount)
    {
        if (transaction.Type != TransactionType.Transfer)
            throw new InvalidOperationException("Only transfer transactions can be processed as transfers");

        ProcessTransaction(transaction);
    }

    /// <summary>
    /// Check if account has sufficient balance
    /// </summary>
    public bool HasSufficientBalance(decimal amount) =>
        IsActive && !IsDeleted && Balance.Value >= amount;

    /// <summary>
    /// Activate/Deactivate account
    /// </summary>
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Get account summary
    /// </summary>
    public string GetSummary() =>
        $"{Currency}: {Balance.Value:N0} - {(IsActive ? "Active" : "Inactive")}";

    #region Private Methods

    private void ValidateAmount(Money amount)
    {
        if (amount == null)
            throw new ArgumentNullException(nameof(amount));

        if (amount.Currency != Currency)
            throw new ArgumentException($"Amount currency {amount.Currency} does not match account currency {Currency}");

        if (amount.Value <= 0)
            throw new ArgumentException("Amount must be positive");
    }

    private void EnsureActive()
    {
        if (!IsActive)
            throw new InvalidOperationException("Account is not active");

        if (IsDeleted)
            throw new InvalidOperationException("Account has been deleted");
    }

    private void EmitTransactionEvent(Transaction transaction)
    {
        switch (transaction.Type)
        {
            case TransactionType.Deposit:
                AddDomainEvent(new WalletDepositedEvent(
                    WalletId, Id, transaction.Amount.Value, Currency,
                    transaction.PaymentReferenceId ?? string.Empty));
                break;

            case TransactionType.Purchase:
                AddDomainEvent(new WalletWithdrawnEvent(
                    WalletId, Id, transaction.Amount.Value, Currency,
                    transaction.OrderContext));
                break;

            case TransactionType.Refund:
                AddDomainEvent(new RefundCompletedEvent(
                    transaction.RelatedTransactionId ?? Guid.Empty,
                    transaction.Id, WalletId, transaction.Amount, Balance.Value));
                break;

            case TransactionType.Transfer:
                if (transaction.Direction == TransactionDirection.Out)
                {
                    AddDomainEvent(new TransferInitiatedEvent(
                        transaction.Id, WalletId, transaction.Amount,
                        transaction.OrderContext ?? "Unknown"));
                }
                else
                {
                    AddDomainEvent(new TransferCompletedEvent(
                        transaction.RelatedTransactionId ?? Guid.Empty,
                        transaction.Id, WalletId, transaction.Amount, Balance.Value));
                }
                break;
        }
    }

    #endregion
}