using BuildingBlocks.CQRS;
using BuildingBlocks.ValueObjects;
using Microsoft.EntityFrameworkCore;
using WalletApp.Domain.Aggregates.TransactionAggregate;
using WalletApp.Domain.Common.Contracts;
using WalletApp.Domain.Exceptions;
using WalletApp.Domain.ValueObjects;

namespace WalletApp.Application.Features.Command.Transactions.TransferMoney;

/// <summary>
/// Transfer money handler
/// </summary>
public class TransferMoneyHandler(IUnitOfWork unitOfWork) : ICommandHandler<TransferMoneyCommand, TransferMoneyResult>
{
    public async Task<TransferMoneyResult> Handle(TransferMoneyCommand request, CancellationToken cancellationToken)
    {
        if (request.FromUserId == request.ToUserId)
        {
            return new TransferMoneyResult
            {
                IsSuccessful = false,
                ErrorMessage = "نمی‌توان به همین کیف پول انتقال داد"
            };
        }

        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // Get sender wallet using optimized query
            var fromWallet = await unitOfWork.Wallets
                .FirstOrDefaultWithIncludesAsync(
                    w => w.UserId == request.FromUserId && !w.IsDeleted,
                    q => q.Include(w => w.CurrencyAccounts),
                    track: true,
                    ct);

            if (fromWallet == null)
            {
                throw new WalletNotFoundException(request.FromUserId);
            }

            // Get receiver wallet using optimized query
            var toWallet = await unitOfWork.Wallets
                .FirstOrDefaultWithIncludesAsync(
                    w => w.UserId == request.ToUserId && !w.IsDeleted,
                    q => q.Include(w => w.CurrencyAccounts),
                    track: true,
                    ct);

            if (toWallet == null)
            {
                return new TransferMoneyResult
                {
                    IsSuccessful = false,
                    ErrorMessage = "کیف پول مقصد یافت نشد"
                };
            }

            if (!toWallet.IsActive)
            {
                return new TransferMoneyResult
                {
                    IsSuccessful = false,
                    ErrorMessage = "کیف پول مقصد غیرفعال است"
                };
            }

            // Get sender account
            var fromAccount = fromWallet.GetCurrencyAccount(request.Currency);
            if (fromAccount == null)
            {
                return new TransferMoneyResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"حساب ارزی {request.Currency} در کیف پول مبدا یافت نشد"
                };
            }

            // Get or create receiver account
            var toAccount = toWallet.GetCurrencyAccount(request.Currency);
            if (toAccount == null)
            {
                toAccount = toWallet.CreateCurrencyAccount(request.Currency);
            }

            var transferAmount = Money.Create(request.Amount, request.Currency);

            // Calculate transfer fee (0.5% with min/max limits)
            var feeAmount = CalculateTransferFee(transferAmount);
            var totalDebitAmount = Money.Create(transferAmount.Value + feeAmount.Value, request.Currency);

            // Check sufficient balance
            if (!fromAccount.HasSufficientBalance(totalDebitAmount.Value))
            {
                return new TransferMoneyResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"موجودی کافی نیست. مبلغ مورد نیاز: {totalDebitAmount.Value:N0} (شامل کارمزد {feeAmount.Value:N0})"
                };
            }

            // Generate transfer reference
            var transferReference = request.Reference ?? $"TRF-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

            // Create outgoing transaction (from sender)
            var fromTransaction = Transaction.CreateTransferOutTransaction(
                fromWallet.Id,
                fromAccount.Id,
                request.FromUserId,
                totalDebitAmount,
                $"انتقال به کاربر {request.ToUserId} - {request.Description}",
                transferReference);

            // Create incoming transaction (to receiver)  
            var toTransaction = Transaction.CreateTransferInTransaction(
                toWallet.Id,
                toAccount.Id,
                request.ToUserId,
                transferAmount,
                $"دریافت از کاربر {request.FromUserId} - {request.Description}",
                transferReference);

            // Link transactions
            fromTransaction.SetRelatedTransaction(toTransaction.Id);
            toTransaction.SetRelatedTransaction(fromTransaction.Id);

            // Process transactions
            fromAccount.ProcessTransfer(fromTransaction, totalDebitAmount);
            toAccount.ProcessTransfer(toTransaction, transferAmount);

            // Save transactions
            await unitOfWork.Transactions.AddAsync(fromTransaction, ct);
            await unitOfWork.Transactions.AddAsync(toTransaction, ct);

            // Create fee transaction if fee > 0
            if (feeAmount.Value > 0)
            {
                var feeTransaction = Transaction.CreateFeeTransaction(
                    fromWallet.Id,
                    fromAccount.Id,
                    request.FromUserId,
                    feeAmount,
                    $"کارمزد انتقال - {transferReference}",
                    fromTransaction.Id);

                await unitOfWork.Transactions.AddAsync(feeTransaction, ct);
            }

            await unitOfWork.SaveChangesAsync(ct);

            return new TransferMoneyResult
            {
                IsSuccessful = true,
                FromTransactionId = fromTransaction.Id,
                ToTransactionId = toTransaction.Id,
                TransferAmount = transferAmount.Value,
                TransferFee = feeAmount.Value,
                FromWalletNewBalance = fromAccount.Balance.Value,
                ToWalletNewBalance = toAccount.Balance.Value,
                TransferReference = transferReference,
                ProcessedAt = DateTime.UtcNow
            };
        }, cancellationToken);
    }

    private static Money CalculateTransferFee(Money amount)
    {
        // Transfer fee: 0.5% with min 1000 IRR and max 50000 IRR
        var feeRate = 0.005m; // 0.5%
        var minFee = 1000m;
        var maxFee = 50000m;

        var calculatedFee = amount.Value * feeRate;
        var actualFee = Math.Max(minFee, Math.Min(maxFee, calculatedFee));

        return Money.Create(actualFee, amount.Currency);
    }
}