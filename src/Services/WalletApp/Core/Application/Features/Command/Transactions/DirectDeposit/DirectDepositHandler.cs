using BuildingBlocks.Contracts;
using BuildingBlocks.ValueObjects;
using Microsoft.EntityFrameworkCore;
using WalletApp.Application.Common.Interfaces;
using WalletApp.Domain.Common;
using WalletApp.Domain.Common.Contracts;
using WalletApp.Domain.Exceptions;

namespace WalletApp.Application.Features.Command.Transactions.DirectDeposit;

/// <summary>
/// Direct deposit handler
/// </summary>
public class DirectDepositHandler(
    IUnitOfWork unitOfWork,
    IPaymentGatewayClient paymentGateway,
    ICurrentUserService userService) : ICommandHandler<DirectDepositCommand, DirectDepositResult>
{
    public async Task<DirectDepositResult> Handle(DirectDepositCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId();

        // Get wallet using optimized query
        var wallet = await unitOfWork.Wallets
            .FirstOrDefaultWithIncludesAsync(
                w => w.UserId == userId && !w.IsDeleted,
                q => q.Include(w => w.CurrencyAccounts),
                track: true,
                cancellationToken);

        if (wallet == null)
        {
            throw new WalletNotFoundException(userId);
        }

        if (!wallet.IsActive)
        {
            return new DirectDepositResult
            {
                IsSuccessful = false,
                ErrorMessage = DomainErrors.GetMessage(DomainErrors.Wallet.Inactive)
            };
        }

        // Get or create currency account
        var account = wallet.GetCurrencyAccount(request.Currency);
        if (account == null)
        {
            account = wallet.CreateCurrencyAccount(request.Currency);
        }

        var money = Money.Create(request.Amount, request.Currency);

        // Create pending transaction
        var transaction = account.CreateDepositTransaction(
            money,
            request.Description);

        // Save pending transaction
        await unitOfWork.Transactions.AddAsync(transaction, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Create payment request
        var paymentResult = await paymentGateway.CreatePaymentAsync(
            money,
            request.Description,            
            orderId: transaction.Id.ToString(),
            gateway: request.PaymentGateway,
            cancellationToken: cancellationToken);

        if (!paymentResult.IsSuccessful)
        {
            // Mark transaction as failed
            transaction.MarkAsFailed(paymentResult.ErrorMessage);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new DirectDepositResult
            {
                IsSuccessful = false,
                ErrorMessage = paymentResult.ErrorMessage ?? "خطا در ایجاد درخواست پرداخت"
            };
        }

        // Update transaction with payment authority
        transaction.SetPaymentReference(paymentResult.Authority!);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new DirectDepositResult
        {
            IsSuccessful = true,
            PaymentUrl = paymentResult.PaymentUrl,
            Authority = paymentResult.Authority,
            PendingTransactionId = transaction.Id
        };
    }
}