﻿
using BuildingBlocks.Enums;
using BuildingBlocks.ValueObjects;
using WalletApp.Domain.Aggregates.TransactionAggregate;
using WalletApp.Domain.Enums;

namespace WalletApp.Domain.DomainServices;

/// <summary>
/// Transaction domain service interface
/// </summary>
public interface ITransactionDomainService
{
    /// <summary>
    /// Check if transaction can be refunded
    /// </summary>
    Task<bool> CanRefundAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate transfer fee based on amount
    /// </summary>
    Money CalculateTransferFee(Money amount);

    /// <summary>
    /// Check if daily transaction limit exceeded
    /// </summary>
    Task<bool> HasDailyLimitExceededAsync(
        Guid userId,
        Money amount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate transaction type for direction
    /// </summary>
    bool IsValidTransactionTypeForDirection(TransactionType type, TransactionDirection direction);

    /// <summary>
    /// Get transaction aging in days
    /// </summary>
    int GetTransactionAgeInDays(Transaction transaction);

    /// <summary>
    /// Check if transaction is eligible for specific operation
    /// </summary>
    bool IsEligibleForOperation(Transaction transaction, TransactionOperation operation);
}

/// <summary>
/// Transaction operations enumeration
/// </summary>
public enum TransactionOperation
{
    Refund = 1,
    Modification = 2,
    Cancellation = 3
}