// Application/Features/Refunds/RefundService.cs
using BuildingBlocks.Enums;
using BuildingBlocks.Messaging.Contracts;
using BuildingBlocks.ValueObjects;
using Microsoft.EntityFrameworkCore;
using WalletApp.Domain.Aggregates.TransactionAggregate;
using WalletApp.Domain.Common.Contracts;
using WalletApp.Domain.Enums;
using WalletApp.Domain.Events;
using WalletApp.Domain.Exceptions;
using WalletApp.Domain.ValueObjects;

namespace WalletApp.Application.Features.Services.Refunds;

/// <summary>
/// Complete Refund Service Implementation
/// Handles two types of refunds:
/// 1. Order cancellation → Money returns to wallet
/// 2. Wallet to bank account → Manual process (future: automated)
/// </summary>
public interface IRefundService
{
    /// <summary>
    /// Process refund from Order service (cancelled tickets)
    /// </summary>
    Task<RefundResult> ProcessOrderRefundAsync(
        Guid userId,
        Guid originalTransactionId,
        decimal refundAmount,
        string reason,
        string? orderNumber = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process wallet to bank account refund (manual process)
    /// </summary>
    Task<RefundResult> ProcessWalletToBankRefundAsync(
        Guid userId,
        Guid bankAccountId,
        decimal amount,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get refundable transactions for user
    /// </summary>
    Task<IEnumerable<RefundableTransactionDto>> GetRefundableTransactionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public class RefundService(IUnitOfWork unitOfWork, IMessageBus messageBus) : IRefundService
{
    /// <summary>
    /// Type 1: Order Refund - Money returns to wallet
    /// Used when: Order cancelled, ticket refunded, etc.
    /// </summary>
    public async Task<RefundResult> ProcessOrderRefundAsync(
        Guid userId,
        Guid originalTransactionId,
        decimal refundAmount,
        string reason,
        string? orderNumber = null,
        CancellationToken cancellationToken = default)
    {
        // Get original transaction
        var originalTransaction = await unitOfWork.Transactions.GetByIdAsync(
            originalTransactionId, track: true, cancellationToken);

        if (originalTransaction == null)
        {
            return RefundResult.Failed("تراکنش اصلی یافت نشد");
        }

        // Validate transaction ownership
        if (originalTransaction.UserId != userId)
        {
            return RefundResult.Failed("شما مجاز به استرداد این تراکنش نیستید");
        }

        // Validate refund eligibility
        if (!originalTransaction.IsRefundable())
        {
            return RefundResult.Failed("این تراکنش قابل استرداد نیست");
        }

        // Validate refund amount
        if (refundAmount > originalTransaction.Amount.Value)
        {
            return RefundResult.Failed("مبلغ استرداد نمی‌تواند بیش از مبلغ اصلی باشد");
        }

        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // Get wallet and currency account using optimized query
            var wallet = await unitOfWork.Wallets
                .FirstOrDefaultWithIncludesAsync(
                    w => w.UserId == userId && !w.IsDeleted,
                    q => q.Include(w => w.CurrencyAccounts),
                    track: true,
                    ct);

            if (wallet == null)
            {
                throw new WalletNotFoundException(userId);
            }

            var account = wallet.GetCurrencyAccount(originalTransaction.Amount.Currency);
            if (account == null)
            {
                throw new InvalidCurrencyAccountException(originalTransaction.Amount.Currency);
            }

            // Create refund transaction
            var refundMoney = Money.Create(refundAmount, originalTransaction.Amount.Currency);
            var refundTransaction = Transaction.CreateRefundTransaction(
                wallet.Id,
                account.Id,
                userId,
                refundMoney,
                $"استرداد سفارش {orderNumber} - {reason}",
                originalTransaction.Id);

            // Process the refund (add money to wallet)
            account.ProcessRefund(refundTransaction);

            // Save changes
            await unitOfWork.Transactions.AddAsync(refundTransaction, ct);
            await unitOfWork.SaveChangesAsync(ct);

            // Publish domain event
            await messageBus.PublishAsync(new RefundCompletedEvent(
                originalTransaction.Id,
                refundTransaction.Id,
                wallet.Id,
                refundMoney,
                account.Balance.Value), ct);

            return RefundResult.Success(
                refundTransaction.Id,
                originalTransaction.Id,
                refundAmount,
                account.Balance.Value);

        }, cancellationToken);
    }

    /// <summary>
    /// Type 2: Wallet to Bank Refund - Manual process for now
    /// Used when: User wants to withdraw money to their bank account
    /// </summary>
    public async Task<RefundResult> ProcessWalletToBankRefundAsync(
        Guid userId,
        Guid bankAccountId,
        decimal amount,
        string reason,
        CancellationToken cancellationToken = default)
    {
        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // Get wallet with optimized includes
            var wallet = await unitOfWork.Wallets
                .FirstOrDefaultWithIncludesAsync(
                    w => w.UserId == userId && !w.IsDeleted,
                    q => q.Include(w => w.CurrencyAccounts)
                          .Include(w => w.BankAccounts.Where(ba => !ba.IsDeleted)),
                    track: true,
                    ct);

            if (wallet == null)
            {
                throw new WalletNotFoundException(userId);
            }

            // Validate bank account
            var bankAccount = wallet.BankAccounts
                .FirstOrDefault(ba => ba.Id == bankAccountId);

            if (bankAccount == null)
            {
                return RefundResult.Failed("حساب بانکی یافت نشد");
            }

            if (!bankAccount.IsActive)
            {
                return RefundResult.Failed("حساب بانکی غیرفعال است");
            }

            // Get IRR currency account
            var account = wallet.GetCurrencyAccount(CurrencyCode.IRR);
            if (account == null)
            {
                return RefundResult.Failed("حساب ریالی یافت نشد");
            }

            // Check sufficient balance
            if (account.Balance.Value < amount)
            {
                return RefundResult.Failed($"موجودی کافی نیست. موجودی فعلی: {account.Balance.Value:N0} ریال");
            }

            var withdrawMoney = Money.Create(amount, CurrencyCode.IRR);

            // Create withdrawal transaction
            var withdrawalTransaction = Transaction.CreatePurchaseTransaction(
                wallet.Id,
                account.Id,
                userId,
                withdrawMoney,
                $"برداشت به حساب {bankAccount.GetMaskedAccountNumber()} - {reason}");

            // Process withdrawal (deduct from wallet)
            account.ProcessPurchase(withdrawalTransaction);

            // Save transaction
            await unitOfWork.Transactions.AddAsync(withdrawalTransaction, ct);
            await unitOfWork.SaveChangesAsync(ct);

            // TODO: In future, integrate with bank APIs for automatic transfer
            // For now, this is a manual process handled by finance team

            return RefundResult.Success(
                withdrawalTransaction.Id,
                null, // No original transaction
                amount,
                account.Balance.Value,
                isManualProcess: true,
                bankAccountInfo: $"{bankAccount.BankName} - {bankAccount.GetMaskedAccountNumber()}");

        }, cancellationToken);
    }

    /// <summary>
    /// Get refundable transactions for user
    /// </summary>
    public async Task<IEnumerable<RefundableTransactionDto>> GetRefundableTransactionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var refundableTransactions = await unitOfWork.Transactions
            .GetRefundableTransactionsAsync(userId, cancellationToken);

        return refundableTransactions.Select(t => new RefundableTransactionDto
        {
            Id = t.Id,
            TransactionNumber = t.TransactionNumber.Value,
            Amount = t.Amount.Value,
            Currency = t.Amount.Currency,
            Description = t.Description,
            TransactionDate = t.TransactionDate,
            OrderContext = t.OrderContext,
            MaxRefundAmount = t.Amount.Value, // Can be partial
            DaysUntilExpiry = 30 - (DateTime.UtcNow - t.ProcessedAt!.Value).Days
        });
    }
}

// DTOs and Result classes
public record RefundResult
{
    public bool IsSuccessful { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? RefundTransactionId { get; init; }
    public Guid? OriginalTransactionId { get; init; }
    public decimal RefundAmount { get; init; }
    public decimal NewWalletBalance { get; init; }
    public bool IsManualProcess { get; init; }
    public string? BankAccountInfo { get; init; }
    public DateTime? ProcessedAt { get; init; }

    public static RefundResult Success(
        Guid refundTransactionId,
        Guid? originalTransactionId,
        decimal refundAmount,
        decimal newBalance,
        bool isManualProcess = false,
        string? bankAccountInfo = null)
    {
        return new RefundResult
        {
            IsSuccessful = true,
            RefundTransactionId = refundTransactionId,
            OriginalTransactionId = originalTransactionId,
            RefundAmount = refundAmount,
            NewWalletBalance = newBalance,
            IsManualProcess = isManualProcess,
            BankAccountInfo = bankAccountInfo,
            ProcessedAt = DateTime.UtcNow
        };
    }

    public static RefundResult Failed(string errorMessage)
    {
        return new RefundResult
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage
        };
    }
}

public record RefundableTransactionDto
{
    public Guid Id { get; init; }
    public string TransactionNumber { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public CurrencyCode Currency { get; init; }
    public string Description { get; init; } = string.Empty;
    public DateTime TransactionDate { get; init; }
    public string? OrderContext { get; init; }
    public decimal MaxRefundAmount { get; init; }
    public int DaysUntilExpiry { get; init; }
}