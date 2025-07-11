//TODO impliment endpoint
using BuildingBlocks.ValueObjects;
using Microsoft.EntityFrameworkCore;
using WalletApp.Domain.Common.Contracts;
using WalletApp.Domain.Exceptions;

namespace WalletApp.Application.Features.Command.Transactions.ProcessWithdrawal;

/// <summary>
/// Handler for processing withdrawal (purchase) transactions
/// SOLID: Single responsibility - only handles withdrawal processing
/// </summary>
public class ProcessWithdrawalHandler(IUnitOfWork unitOfWork, ILogger<ProcessWithdrawalHandler> logger)
    : ICommandHandler<ProcessWithdrawalCommand, ProcessWithdrawalResponse>
{
    public async Task<ProcessWithdrawalResponse> Handle(
        ProcessWithdrawalCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Processing withdrawal for UserId: {UserId}, Amount: {Amount} {Currency}, OrderContext: {OrderContext}",
                request.UserId, request.Amount, request.Currency, request.OrderContext);

            // Find user's wallet using optimized query
            var wallet = await unitOfWork.Wallets
                .FirstOrDefaultWithIncludesAsync(
                    w => w.UserId == request.UserId && !w.IsDeleted,
                    q => q.Include(w => w.CurrencyAccounts),
                    track: true,
                    cancellationToken);

            if (wallet == null)
            {
                logger.LogWarning("Wallet not found for UserId: {UserId}", request.UserId);
                return ProcessWithdrawalResponse.Failure("کیف پول کاربر یافت نشد", "WALLET_NOT_FOUND");
            }

            // Get currency account
            var currencyAccount = wallet.GetCurrencyAccount(request.Currency);
            if (currencyAccount == null)
            {
                logger.LogWarning("Currency account not found for UserId: {UserId}, Currency: {Currency}",
                    request.UserId, request.Currency);
                return ProcessWithdrawalResponse.Failure($"حساب {request.Currency} یافت نشد", "CURRENCY_ACCOUNT_NOT_FOUND");
            }

            // Check if account is active
            if (!currencyAccount.IsActive)
            {
                logger.LogWarning("Currency account is inactive for UserId: {UserId}, Currency: {Currency}",
                    request.UserId, request.Currency);
                return ProcessWithdrawalResponse.Failure("حساب غیرفعال است", "ACCOUNT_INACTIVE");
            }

            // Create money object
            var withdrawalAmount = Money.Create(request.Amount, request.Currency);

            // Check sufficient balance
            if (!currencyAccount.HasSufficientBalance(withdrawalAmount.Value))
            {
                logger.LogWarning(
                    "Insufficient balance for UserId: {UserId}, Required: {Required}, Available: {Available}",
                    request.UserId, withdrawalAmount.Value, currencyAccount.Balance.Value);

                return ProcessWithdrawalResponse.Failure(
                    $"موجودی ناکافی. موجودی فعلی: {currencyAccount.Balance.Value:N0} {request.Currency}",
                    "INSUFFICIENT_BALANCE");
            }

            // Create purchase transaction
            var transaction = currencyAccount.CreatePurchaseTransaction(
                withdrawalAmount,
                request.Description,
                request.OrderContext);

            // Set external reference if provided
            if (!string.IsNullOrEmpty(request.ExternalReference))
            {
                transaction.SetPaymentReference(request.ExternalReference);
            }

            // Add transaction to repository
            await unitOfWork.Transactions.AddAsync(transaction, cancellationToken);

            // Process the withdrawal (update balance)
            currencyAccount.ProcessPurchase(transaction);

            // Save changes
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Withdrawal processed successfully. UserId: {UserId}, TransactionId: {TransactionId}, " +
                "Amount: {Amount} {Currency}, RemainingBalance: {Balance}",
                request.UserId, transaction.Id, withdrawalAmount.Value, request.Currency, currencyAccount.Balance.Value);

            return ProcessWithdrawalResponse.Success(
                transaction.Id,
                transaction.TransactionNumber.Value,
                currencyAccount.Balance.Value);
        }
        catch (InsufficientBalanceException ex)
        {
            logger.LogWarning(ex, "Insufficient balance for withdrawal. UserId: {UserId}, Amount: {Amount}",
                request.UserId, request.Amount);

            return ProcessWithdrawalResponse.Failure(
                $"موجودی ناکافی. مبلغ درخواستی: {request.Amount:N0}، موجودی قابل استفاده: {ex.AvailableBalance:N0}",
                "INSUFFICIENT_BALANCE");
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation for withdrawal. UserId: {UserId}", request.UserId);

            return ProcessWithdrawalResponse.Failure(
                "عملیات نامعتبر: " + ex.Message,
                "INVALID_OPERATION");
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid argument for withdrawal. UserId: {UserId}", request.UserId);

            return ProcessWithdrawalResponse.Failure(
                "پارامتر نامعتبر: " + ex.Message,
                "INVALID_ARGUMENT");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing withdrawal for UserId: {UserId}", request.UserId);

            return ProcessWithdrawalResponse.Failure(
                "خطا در پردازش برداشت",
                "INTERNAL_ERROR");
        }
    }
}