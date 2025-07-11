using BuildingBlocks.Domain;
using BuildingBlocks.ValueObjects;
using WalletApp.Domain.Enums;

namespace WalletApp.Domain.Aggregates.TransactionAggregate;

/// <summary>
/// Transaction Snapshot for balance history and reporting
/// </summary>
public class TransactionSnapshot : BaseEntity<long>
{
    public Guid AccountId { get; private set; }
    public Money Balance { get; private set; } = null!;
    public DateTime SnapshotDate { get; private set; }
    public SnapshotType Type { get; private set; }
    public Guid? TransactionId { get; private set; }

    // Private constructor for EF Core
    private TransactionSnapshot() { }

    /// <summary>
    /// Create snapshot
    /// </summary>
    private TransactionSnapshot(Guid accountId, Money balance, SnapshotType type, Guid? transactionId = null)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("AccountId cannot be empty");

        AccountId = accountId;
        Balance = balance ?? throw new ArgumentNullException(nameof(balance));
        Type = type;
        SnapshotDate = DateTime.UtcNow;
        TransactionId = transactionId;
    }

    /// <summary>
    /// Factory methods
    /// </summary>
    public static TransactionSnapshot CreateDailySnapshot(Guid accountId, Money balance) =>
        new(accountId, balance, SnapshotType.Daily);

    public static TransactionSnapshot CreateManualSnapshot(Guid accountId, Money balance, Guid transactionId) =>
        new(accountId, balance, SnapshotType.Manual, transactionId);
}