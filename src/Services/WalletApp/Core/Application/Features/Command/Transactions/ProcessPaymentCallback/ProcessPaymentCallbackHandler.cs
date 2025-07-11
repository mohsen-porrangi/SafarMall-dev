using Microsoft.EntityFrameworkCore;
using WalletApp.Application.Common.Interfaces;
using WalletApp.Domain.Common.Contracts;
using WalletApp.Domain.Enums;
using WalletApp.Domain.Exceptions;

namespace WalletApp.Application.Features.Command.Transactions.ProcessPaymentCallback;

/// <summary>
/// Payment callback handler
/// </summary>
public class ProcessPaymentCallbackHandler(
    IUnitOfWork unitOfWork, 
    IPaymentGatewayClient paymentGateway,
    IOrderServiceClient orderServiceClient,
    ILogger<ProcessPaymentCallbackHandler> logger)
    : ICommandHandler<ProcessPaymentCallbackCommand, PaymentCallbackResult>
{
    public async Task<PaymentCallbackResult> Handle(ProcessPaymentCallbackCommand request, CancellationToken cancellationToken)
    {
        // Find transaction by payment authority
        var transaction = await unitOfWork.Transactions
            .FirstOrDefaultAsync(t => t.PaymentReferenceId == request.Authority, track: true, cancellationToken);

        if (transaction == null)
        {
            return new PaymentCallbackResult
            {
                IsSuccessful = false,
                ErrorMessage = "تراکنش مرتبط با این پرداخت یافت نشد"
            };
        }

        // Check if already processed
        if (transaction.Status == TransactionStatus.Completed)
        {
            // Get wallet with currency accounts for balance info using optimized query
            var wallet = await unitOfWork.Wallets
                .FirstOrDefaultWithIncludesAsync(
                    w => w.Id == transaction.WalletId && !w.IsDeleted,
                    q => q.Include(w => w.CurrencyAccounts),
                    cancellationToken: cancellationToken);

            var account = wallet?.GetCurrencyAccount(transaction.Amount.Currency);

            return new PaymentCallbackResult
            {
                IsSuccessful = true,
                IsVerified = true,
                TransactionId = transaction.Id,
                WalletId = transaction.WalletId,
                Amount = transaction.Amount.Value,
                Currency = transaction.Amount.Currency,
                NewBalance = account?.Balance.Value,
                ReferenceId = transaction.PaymentReferenceId,
                ProcessedAt = transaction.ProcessedAt
            };
        }

        // Handle failed payment
        if (request.Status != "OK")
        {
            transaction.MarkAsFailed($"Payment failed with status: {request.Status}");
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new PaymentCallbackResult
            {
                IsSuccessful = false,
                TransactionId = transaction.Id,
                WalletId = transaction.WalletId,
                ErrorMessage = "پرداخت ناموفق بود"
            };
        }

        // Verify payment with gateway
        var verificationResult = await paymentGateway.VerifyPaymentAsync(
            request.Authority,
            transaction.Amount,
            request.Gateway,
            cancellationToken);

        if (!verificationResult.IsSuccessful || !verificationResult.IsVerified)
        {
            transaction.MarkAsFailed(verificationResult.ErrorMessage ?? "Payment verification failed");
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new PaymentCallbackResult
            {
                IsSuccessful = false,
                TransactionId = transaction.Id,
                WalletId = transaction.WalletId,
                ErrorMessage = verificationResult.ErrorMessage ?? "تأیید پرداخت ناموفق بود"
            };
        }

        // Validate amount
        if (request.Amount.HasValue && Math.Abs(transaction.Amount.Value - request.Amount.Value) > 0.01m)
        {
            transaction.MarkAsFailed($"Amount mismatch: expected {transaction.Amount.Value}, received {request.Amount.Value}");
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new PaymentCallbackResult
            {
                IsSuccessful = false,
                TransactionId = transaction.Id,
                ErrorMessage = "مبلغ پرداخت با مبلغ درخواستی مطابقت ندارد"
            };
        }

        // Execute transaction in database transaction
        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // Get wallet with currency account using optimized query
            var walletWithAccount = await unitOfWork.Wallets
                .FirstOrDefaultWithIncludesAsync(
                    w => w.UserId == transaction.UserId && !w.IsDeleted,
                    q => q.Include(w => w.CurrencyAccounts),
                    track: true,
                    ct);

            if (walletWithAccount == null)
            {
                throw new WalletNotFoundException(transaction.UserId);
            }

            var account = walletWithAccount.GetCurrencyAccount(transaction.Amount.Currency);
            if (account == null)
            {
                throw new InvalidCurrencyAccountException(transaction.Amount.Currency);
            }

            // Process the deposit
            account.ProcessDeposit(transaction);

            // Mark transaction as completed
            transaction.MarkAsCompleted();

            // Set verification reference
            if (!string.IsNullOrEmpty(verificationResult.ReferenceId))
            {
                transaction.SetPaymentReference(verificationResult.ReferenceId);
            }
            // CRITICAL: Complete order if this was a purchase
            if (!string.IsNullOrEmpty(transaction.OrderContext))
            {
                var orderCompleted = await orderServiceClient.CompleteOrderAsync(
                    transaction.OrderContext, ct);

                if (!orderCompleted)
                {
                    logger.LogWarning("Failed to complete order: {OrderId} for transaction: {TransactionId}",
                        transaction.OrderContext, transaction.Id);
                }
            }

            // Save changes
            await unitOfWork.SaveChangesAsync(ct);

            return new PaymentCallbackResult
            {
                IsSuccessful = true,
                IsVerified = true,
                TransactionId = transaction.Id,
                WalletId = transaction.WalletId,
                Amount = transaction.Amount.Value,
                Currency = transaction.Amount.Currency,
                NewBalance = account.Balance.Value,
                ReferenceId = verificationResult.ReferenceId,
                ProcessedAt = DateTime.UtcNow
            };
        }, cancellationToken);
    }
}