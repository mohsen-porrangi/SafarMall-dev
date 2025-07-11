using BuildingBlocks.Contracts;
using BuildingBlocks.Domain;
using BuildingBlocks.Enums;
using BuildingBlocks.ValueObjects;
using WalletApp.Domain.Aggregates.WalletAggregate;
using WalletApp.Domain.Enums;
using WalletApp.Domain.Events;
using WalletApp.Domain.ValueObjects;

namespace WalletApp.Domain.Aggregates.TransactionAggregate;

/// <summary>
/// Transaction Entity - Core business entity for all financial operations
/// </summary>
public class Transaction : EntityWithDomainEvents<Guid>, IAggregateRoot , ISoftDelete
{
    private TransactionNumber _transactionNumber = null!;
    private Money _amount = null!;

    public TransactionNumber TransactionNumber
    {
        get => _transactionNumber;
        private set => _transactionNumber = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Guid WalletId { get; private set; }
    public Guid CurrencyAccountId { get; private set; }
    public Guid? RelatedTransactionId { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public Money Amount
    {
        get => _amount;
        private set => _amount = value ?? throw new ArgumentNullException(nameof(value));
    }

    public TransactionDirection Direction { get; private set; }
    public TransactionType Type { get; private set; }
    public TransactionStatus Status { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public bool IsCredit { get; private set; }
    public DateTime? DueDate { get; private set; }
    public string? PaymentReferenceId { get; private set; }
    public string? OrderContext { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    // Navigation properties
    public virtual Wallet Wallet { get; private set; } = null!;
    public virtual CurrencyAccount CurrencyAccount { get; private set; } = null!;

    // Private constructor for EF Core
    private Transaction() { }

    #region Factory Methods

    /// <summary>
    /// Generic factory method to reduce code duplication
    /// </summary>
    private static Transaction Create(
        Guid walletId,
        Guid currencyAccountId,
        Guid userId,
        Money amount,
        TransactionDirection direction,
        TransactionType type,
        string description,
        string? referenceId = null,
        Guid? relatedTransactionId = null,
        bool isCredit = false,
        DateTime? dueDate = null)
    {
        return new Transaction(
            walletId, currencyAccountId, userId, amount,
            direction, type, description, isCredit,
            dueDate, referenceId, relatedTransactionId, referenceId);
    }

    public static Transaction CreateDepositTransaction(
        Guid walletId, Guid currencyAccountId, Guid userId,
        Money amount, string description, string? paymentReferenceId = null) =>
        Create(walletId, currencyAccountId, userId, amount,
            TransactionDirection.In, TransactionType.Deposit, description, paymentReferenceId);

    public static Transaction CreatePurchaseTransaction(
        Guid walletId, Guid currencyAccountId, Guid userId,
        Money amount, string description, string? orderContext = null) =>
        Create(walletId, currencyAccountId, userId, amount,
            TransactionDirection.Out, TransactionType.Purchase, description, orderContext);

    public static Transaction CreateRefundTransaction(
        Guid walletId, Guid currencyAccountId, Guid userId,
        Money amount, string description, Guid originalTransactionId) =>
        Create(walletId, currencyAccountId, userId, amount,
            TransactionDirection.In, TransactionType.Refund, description,
            relatedTransactionId: originalTransactionId);

    public static Transaction CreateTransferOutTransaction(
        Guid walletId, Guid currencyAccountId, Guid userId,
        Money amount, string description, string transferReference) =>
        Create(walletId, currencyAccountId, userId, amount,
            TransactionDirection.Out, TransactionType.Transfer, description, transferReference);

    public static Transaction CreateTransferInTransaction(
        Guid walletId, Guid currencyAccountId, Guid userId,
        Money amount, string description, string transferReference) =>
        Create(walletId, currencyAccountId, userId, amount,
            TransactionDirection.In, TransactionType.Transfer, description, transferReference);

    public static Transaction CreateFeeTransaction(
        Guid walletId, Guid currencyAccountId, Guid userId,
        Money feeAmount, string description, Guid relatedTransactionId) =>
        Create(walletId, currencyAccountId, userId, feeAmount,
            TransactionDirection.Out, TransactionType.Fee, description,
            relatedTransactionId: relatedTransactionId);

    public static Transaction CreateCreditTransaction(
        Guid walletId, Guid currencyAccountId, Guid userId,
        Money amount, string description, DateTime dueDate, string? orderContext = null) =>
        Create(walletId, currencyAccountId, userId, amount,
            TransactionDirection.Out, TransactionType.Purchase, description,
            orderContext, isCredit: true, dueDate: dueDate);

    #endregion

    /// <summary>
    /// Private constructor with validation
    /// </summary>
    private Transaction(
        Guid walletId,
        Guid currencyAccountId,
        Guid userId,
        Money amount,
        TransactionDirection direction,
        TransactionType type,
        string description,
        bool isCredit = false,
        DateTime? dueDate = null,
        string? paymentReferenceId = null,
        Guid? relatedTransactionId = null,
        string? orderContext = null)
    {
        // Basic validation
        if (walletId == Guid.Empty || currencyAccountId == Guid.Empty || userId == Guid.Empty)
            throw new ArgumentException("IDs cannot be empty");

        if (amount == null || amount.Value <= 0)
            throw new ArgumentException("Amount must be positive");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required");

        // Set properties
        WalletId = walletId;
        CurrencyAccountId = currencyAccountId;
        UserId = userId;
        Amount = amount;
        Direction = direction;
        Type = type;
        Status = TransactionStatus.Pending;
        Description = description.Trim();
        IsCredit = isCredit;
        DueDate = dueDate;
        PaymentReferenceId = paymentReferenceId;
        RelatedTransactionId = relatedTransactionId;
        OrderContext = orderContext;
        TransactionDate = DateTime.UtcNow;
        TransactionNumber = TransactionNumber.Generate();

        // Emit domain event
        AddDomainEvent(new TransactionInitiatedEvent(
            Id, WalletId, Amount, Direction, Type, OrderContext));
    }

    /// <summary>
    /// Mark transaction as completed
    /// </summary>
    public void MarkAsCompleted()
    {
        if (Status == TransactionStatus.Completed)
            return;

        Status = TransactionStatus.Completed;
        ProcessedAt = DateTime.UtcNow;

        AddDomainEvent(new TransactionCompletedEvent(
            Id, TransactionNumber.Value, WalletId, UserId,
            Amount.Value, Direction, Type, Amount.Currency,
            Description, PaymentReferenceId, OrderContext));
    }

    /// <summary>
    /// Mark transaction as failed
    /// </summary>
    public void MarkAsFailed(string? reason = null)
    {
        if (Status == TransactionStatus.Completed)
            throw new InvalidOperationException("Cannot fail a completed transaction");

        Status = TransactionStatus.Failed;
        ProcessedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(reason))
            Description = $"{Description} - Failed: {reason}";

        AddDomainEvent(new TransactionFailedEvent(Id, WalletId, reason ?? "Unknown error"));
    }

    /// <summary>
    /// Set payment reference
    /// </summary>
    public void SetPaymentReference(string paymentReferenceId)
    {
        if (string.IsNullOrWhiteSpace(paymentReferenceId))
            throw new ArgumentException("Payment reference cannot be empty");

        PaymentReferenceId = paymentReferenceId;
       // Updated();
    }
    /// <summary>
    /// Set order context for completion callback
    /// </summary>
    public void SetOrderContext(string orderContext)
    {
        if (string.IsNullOrWhiteSpace(orderContext))
            throw new ArgumentException("Order context cannot be empty", nameof(orderContext));

        OrderContext = orderContext;
       // Updated();
    }
    /// <summary>
    /// Set related transaction
    /// </summary>
    public void SetRelatedTransaction(Guid relatedTransactionId)
    {
        RelatedTransactionId = relatedTransactionId;
    }

    /// <summary>
    /// Check if transaction is refundable
    /// </summary>
    public bool IsRefundable() =>
        Status == TransactionStatus.Completed &&
        Direction == TransactionDirection.Out &&
        Type != TransactionType.Refund &&
        ProcessedAt.HasValue &&
        ProcessedAt.Value > DateTime.UtcNow.AddDays(-30);

    /// <summary>
    /// Get display amount
    /// </summary>
    public decimal GetDisplayAmount() => Math.Abs(Amount.Value);
}