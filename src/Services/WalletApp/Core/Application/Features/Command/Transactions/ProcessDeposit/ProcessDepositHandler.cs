using BuildingBlocks.CQRS;
using BuildingBlocks.Exceptions;
using BuildingBlocks.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WalletApp.Domain.Common.Contracts;

namespace WalletApp.Application.Features.Command.Transactions.ProcessDeposit;

/// <summary>
/// Handler for processing deposit from payment gateway
/// SOLID: Single responsibility - only handles deposit processing
/// </summary>
public class ProcessDepositHandler(IUnitOfWork unitOfWork, ILogger<ProcessDepositHandler> logger)
    : ICommandHandler<ProcessDepositCommand, ProcessDepositResponse>
{
    public async Task<ProcessDepositResponse> Handle(
        ProcessDepositCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Processing deposit for GatewayReference: {GatewayRef}, Amount: {Amount}",
                request.GatewayReference, request.Amount);

            // Find wallet using optimized query
            var wallet = await unitOfWork.Wallets
                .FirstOrDefaultWithIncludesAsync(
                    w => w.UserId == request.UserId && !w.IsDeleted,
                    q => q.Include(w => w.CurrencyAccounts),
                    track: true,
                    cancellationToken);

            if (wallet == null)
            {
                logger.LogError("Wallet not found for GatewayReference: {GatewayRef}", request.GatewayReference);
                return new ProcessDepositResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "کیف پول کاربر یافت نشد"
                };
            }

            // Get or create currency account
            var currencyAccount = wallet.GetCurrencyAccount(request.Currency);
            if (currencyAccount == null)
            {
                currencyAccount = wallet.CreateCurrencyAccount(request.Currency);
            }

            // Create deposit transaction
            var money = Money.Create(request.Amount, request.Currency);
            var transaction = currencyAccount.CreateDepositTransaction(
                money,
                request.Description,
                request.PaymentReferenceId);

            // Add transaction to repository
            await unitOfWork.Transactions.AddAsync(transaction, cancellationToken);

            // Process the deposit (update balance)
            currencyAccount.ProcessDeposit(transaction);

            // Save changes
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Deposit processed successfully. TransactionId: {TransactionId}, NewBalance: {Balance}",
                transaction.Id, currencyAccount.Balance.Value);

            return new ProcessDepositResponse
            {
                IsSuccess = true,
                TransactionId = transaction.Id,
                NewBalance = currencyAccount.Balance.Value
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing deposit for GatewayReference: {GatewayRef}", request.GatewayReference);
            return new ProcessDepositResponse
            {
                IsSuccess = false,
                ErrorMessage = "خطا در پردازش واریز"
            };
        }
    }
}