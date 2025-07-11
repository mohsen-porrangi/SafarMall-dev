using BuildingBlocks.Enums;
using BuildingBlocks.ValueObjects;
using WalletApp.Domain.Aggregates.TransactionAggregate;
using WalletApp.Domain.Common;
using WalletApp.Domain.Common.Contracts;
using WalletApp.Domain.Enums;

namespace WalletApp.Domain.DomainServices;

/// <summary>
/// Transaction domain service implementation
/// Contains core business logic for transaction operations
/// </summary>
public class TransactionDomainService(IUnitOfWork unitOfWork) : ITransactionDomainService
{
    /// <summary>
    /// Check if transaction can be refunded based on business rules
    /// </summary>
    public async Task<bool> CanRefundAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        if (transaction == null)
            return false;

        // Use business rules for basic validation
        if (!BusinessRules.Transaction.CanBeRefunded(
            transaction.Status,
            transaction.Direction,
            transaction.Type,
            transaction.TransactionDate))
        {
            return false;
        }

        // Check if already refunded
        var relatedRefunds = await unitOfWork.Transactions.GetRelatedTransactionsAsync(
            transaction.Id, cancellationToken);

        var totalRefunded = relatedRefunds
            .Where(t => t.Type == TransactionType.Refund)
            .Sum(t => t.Amount.Value);

        // Can't refund if already fully refunded
        return totalRefunded < transaction.Amount.Value;
    }

    /// <summary>
    /// Calculate transfer fee: 0.5% with min/max limits
    /// </summary>
    public Money CalculateTransferFee(Money amount)
    {
        if (amount == null)
            throw new ArgumentNullException(nameof(amount));

        const decimal feeRate = 0.005m; // 0.5%
        const decimal minFee = 1000m;   // 1000 IRR
        const decimal maxFee = 50000m;  // 50000 IRR

        var calculatedFee = amount.Value * feeRate;
        var actualFee = Math.Max(minFee, Math.Min(maxFee, calculatedFee));

        return Money.Create(actualFee, amount.Currency);
    }

    /// <summary>
    /// Check daily transaction limits
    /// </summary>
    public async Task<bool> HasDailyLimitExceededAsync(
        Guid userId,
        Money amount,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        // Get wallet using optimized query
        var wallet = await unitOfWork.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId && !w.IsDeleted, cancellationToken: cancellationToken);

        if (wallet == null)
            return true; // No wallet = limit exceeded

        var dailyUsage = await unitOfWork.Wallets.GetDailyTransactionAmountAsync(
            wallet.Id, amount.Currency, today, cancellationToken);

        return BusinessRules.Amounts.ExceedsDailyLimit(amount, Money.Create(dailyUsage, amount.Currency));
    }

    /// <summary>
    /// Validate transaction type for direction using business rules
    /// </summary>
    public bool IsValidTransactionTypeForDirection(TransactionType type, TransactionDirection direction)
    {
        return BusinessRules.Transaction.IsValidTypeForDirection(type, direction);
    }

    /// <summary>
    /// Get transaction age in days
    /// </summary>
    public int GetTransactionAgeInDays(Transaction transaction)
    {
        if (transaction == null)
            return int.MaxValue;

        var referenceDate = transaction.ProcessedAt ?? transaction.TransactionDate;
        return (DateTime.UtcNow - referenceDate).Days;
    }

    /// <summary>
    /// Check eligibility for specific operations
    /// </summary>
    public bool IsEligibleForOperation(Transaction transaction, TransactionOperation operation)
    {
        if (transaction == null)
            return false;

        return operation switch
        {
            TransactionOperation.Refund => transaction.IsRefundable(),
            TransactionOperation.Modification => CanModifyTransaction(transaction),
            TransactionOperation.Cancellation => CanCancelTransaction(transaction),
            _ => false
        };
    }

    #region Private Helper Methods

    /// <summary>
    /// Check if transaction can be modified
    /// </summary>
    private bool CanModifyTransaction(Transaction transaction)
    {
        // Only pending transactions can be modified
        return transaction.Status == TransactionStatus.Pending &&
               GetTransactionAgeInDays(transaction) <= 1; // Within 1 day
    }

    /// <summary>
    /// Check if transaction can be cancelled
    /// </summary>
    private bool CanCancelTransaction(Transaction transaction)
    {
        // Only pending or processing transactions can be cancelled
        return transaction.Status is TransactionStatus.Pending or TransactionStatus.Processing;
    }

    #endregion
}