using BuildingBlocks.Contracts;
using BuildingBlocks.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using WalletApp.Application.Common.Interfaces;
using WalletApp.Domain.Aggregates.TransactionAggregate;
using WalletApp.Domain.Aggregates.WalletAggregate;
using WalletApp.Domain.Common;
using WalletApp.Domain.Common.Contracts;
using WalletApp.Domain.Exceptions;

namespace WalletApp.Application.Features.Command.Transactions.IntegratedPurchase;

/// <summary>
/// Complete Integrated Purchase Handler 
/// Handles three scenarios:
/// 1. Full wallet payment (موجودی کافی)
/// 2. Mixed payment (موجودی + درگاه)
/// 3. Full gateway payment (بدون موجودی)
/// </summary>
public class IntegratedPurchaseHandler(
    IUnitOfWork unitOfWork, 
    IPaymentGatewayClient paymentGateway,
    ICurrentUserService userService,
    IOrderServiceClient orderServiceClient,
    ILogger<IntegratedPurchaseHandler> logger)
    : ICommandHandler<IntegratedPurchaseCommand, IntegratedPurchaseResult>
{
    private Guid userId = userService.GetCurrentUserId();
    public async Task<IntegratedPurchaseResult> Handle(
        IntegratedPurchaseCommand request,
        CancellationToken cancellationToken)
    {
        // Get wallet with only required includes based on use case
        var includeExpression = BuildIncludeExpression(request.UseCredit);
        var wallet = await unitOfWork.Wallets
            .FirstOrDefaultWithIncludesAsync(
                w => w.UserId == userId && !w.IsDeleted,
                includeExpression,
                track: true,
                cancellationToken);

        if (wallet == null)
        {
            throw new WalletNotFoundException(userId);
        }

        if (!wallet.IsActive)
        {
            return new IntegratedPurchaseResult
            {
                IsSuccessful = false,
                ErrorMessage = DomainErrors.GetMessage(DomainErrors.Wallet.Inactive)
            };
        }

        var purchaseAmount = Money.Create(request.TotalAmount, request.Currency);

        // Validate purchase amount
        if (!BusinessRules.Amounts.IsValidTransactionAmount(purchaseAmount))
        {
            return new IntegratedPurchaseResult
            {
                IsSuccessful = false,
                ErrorMessage = "مبلغ خرید نامعتبر است"
            };
        }

        // Handle B2B credit purchase
        if (request.UseCredit)
        {
            return await HandleCreditPurchaseAsync(wallet, purchaseAmount, request, cancellationToken);
        }

        // Get or create currency account
        var account = wallet.GetCurrencyAccount(request.Currency);
        if (account == null)
        {
            account = wallet.CreateCurrencyAccount(request.Currency);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var currentBalance = account.Balance.Value;

        // Main Business Logic: Determine purchase strategy
        return currentBalance switch
        {
            var balance when balance >= request.TotalAmount =>
                await ProcessFullWalletPurchaseAsync(account, purchaseAmount, request, cancellationToken),

            var balance when balance <= 0 =>
                await ProcessFullPaymentPurchaseAsync(purchaseAmount, request, cancellationToken),

            _ => await ProcessMixedPurchaseAsync(account, purchaseAmount, request, cancellationToken)
        };
    }

    /// <summary>
    /// Build include expression based on requirements
    /// </summary>
    private static Func<IQueryable<Wallet>, IIncludableQueryable<Wallet, object>> BuildIncludeExpression(bool useCredit)
    {
        return q =>
        {
            var query = q.Include(w => w.CurrencyAccounts);
            return useCredit ? query.Include(w => w.Credits) : query;
        };
    }

    /// <summary>
    /// Scenario 1: Full wallet payment (کل پول از کیف پول)
    /// </summary>
    private async Task<IntegratedPurchaseResult> ProcessFullWalletPurchaseAsync(
        CurrencyAccount account,
        Money purchaseAmount,
        IntegratedPurchaseCommand request,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // Create purchase transaction
            var purchaseTransaction = account.CreatePurchaseTransaction(
                purchaseAmount,
                $"خرید {request.Description}",
                request.OrderId);

            // Save transaction FIRST
            await unitOfWork.Transactions.AddAsync(purchaseTransaction, ct);

            // CRITICAL: Process the purchase (this will update balance AND mark as completed)
            account.ProcessPurchase(purchaseTransaction);

            // Save changes (balance update)
            await unitOfWork.SaveChangesAsync(ct);

            // Complete order
            if (!string.IsNullOrEmpty(request.OrderId))
            {
                var orderCompleted = await orderServiceClient.CompleteOrderAsync(
                    request.OrderId, ct);

                if (!orderCompleted)
                {
                    logger.LogWarning("Failed to complete order: {OrderId} for transaction: {TransactionId}",
                        request.OrderId, purchaseTransaction.Id);
                }
            }

            return new IntegratedPurchaseResult
            {
                IsSuccessful = true,
                PurchaseType = PurchaseType.FullWallet,
                TotalAmount = request.TotalAmount,
                WalletBalance = account.Balance.Value,
                RequiredPayment = 0,
                PurchaseTransactionId = purchaseTransaction.Id,
                ProcessedAt = DateTime.UtcNow
            };
        }, cancellationToken);
    }

    /// <summary>
    /// Scenario 2: Full payment gateway (کل پول از درگاه)
    /// </summary>
    private async Task<IntegratedPurchaseResult> ProcessFullPaymentPurchaseAsync(
        Money purchaseAmount,
        IntegratedPurchaseCommand request,
        CancellationToken cancellationToken)
    {
        // Create payment request for full amount
        var paymentResult = await paymentGateway.CreatePaymentAsync(
            purchaseAmount,
            $"خرید {request.Description} - سفارش {request.OrderId}",            
            request.PaymentGateway,
            request.OrderId,
            cancellationToken: cancellationToken);

        if (!paymentResult.IsSuccessful)
        {
            return new IntegratedPurchaseResult
            {
                IsSuccessful = false,
                ErrorMessage = paymentResult.ErrorMessage ?? "خطا در ایجاد درخواست پرداخت"
            };
        }

        return new IntegratedPurchaseResult
        {
            IsSuccessful = true,
            PurchaseType = PurchaseType.FullPayment,
            TotalAmount = request.TotalAmount,
            WalletBalance = 0,
            RequiredPayment = request.TotalAmount,
            PaymentUrl = paymentResult.PaymentUrl,
            Authority = paymentResult.Authority
        };
    }

    /// <summary>
    /// Scenario 3: Mixed purchase (بخشی کیف پول + بخشی درگاه)
    /// Core Business Logic: Use available wallet + gateway for remainder
    /// </summary>
    private async Task<IntegratedPurchaseResult> ProcessMixedPurchaseAsync(
        CurrencyAccount account,
        Money purchaseAmount,
        IntegratedPurchaseCommand request,
        CancellationToken cancellationToken)
    {
        var walletAmount = account.Balance.Value;
        var paymentAmount = request.TotalAmount - walletAmount;

        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // Step 1: Use wallet balance
            var walletPurchaseAmount = Money.Create(walletAmount, request.Currency);
            var walletTransaction = account.CreatePurchaseTransaction(
                walletPurchaseAmount,
                $"خرید جزئی از کیف پول - {request.Description}",
                request.OrderId);

            // Process wallet purchase
            account.ProcessPurchase(walletTransaction);
            await unitOfWork.Transactions.AddAsync(walletTransaction, ct);

            // Step 2: Create pending payment transaction for remaining amount
            var paymentMoney = Money.Create(paymentAmount, request.Currency);
            var paymentTransaction = account.CreateDepositTransaction(
                paymentMoney,
                $"پرداخت باقیمانده - {request.Description}",
                null); // PaymentReference will be set after gateway response

            // CRITICAL: Set order context for completion callback
            paymentTransaction.SetOrderContext(request.OrderId);
            await unitOfWork.Transactions.AddAsync(paymentTransaction, ct);
            await unitOfWork.SaveChangesAsync(ct);

            // Step 3: Create payment request
            var paymentResult = await paymentGateway.CreatePaymentAsync(
                paymentMoney,
                $"تکمیل خرید {request.Description} - سفارش {request.OrderId}",
                request.PaymentGateway,
                paymentTransaction.Id.ToString(), // Use transaction ID as order reference
                cancellationToken: ct);

            if (!paymentResult.IsSuccessful)
            {
                // Transaction will rollback automatically
                return new IntegratedPurchaseResult
                {
                    IsSuccessful = false,
                    ErrorMessage = paymentResult.ErrorMessage ?? "خطا در ایجاد درخواست پرداخت"
                };
            }

            // Step 4: Update payment transaction with gateway reference
            paymentTransaction.SetPaymentReference(paymentResult.Authority!);
            await unitOfWork.SaveChangesAsync(ct);

            return new IntegratedPurchaseResult
            {
                IsSuccessful = true,
                PurchaseType = PurchaseType.Mixed,
                TotalAmount = request.TotalAmount,
                WalletBalance = account.Balance.Value, // Should be 0 now
                RequiredPayment = paymentAmount,
                PurchaseTransactionId = walletTransaction.Id,
                PaymentTransactionId = paymentTransaction.Id, // اضافه شده
                PaymentUrl = paymentResult.PaymentUrl,
                Authority = paymentResult.Authority
            };
        }, cancellationToken);
    }

    /// <summary>
    /// B2B Credit Purchase
    /// </summary>
    private async Task<IntegratedPurchaseResult> HandleCreditPurchaseAsync(
        Wallet wallet,
        Money purchaseAmount,
        IntegratedPurchaseCommand request,
        CancellationToken cancellationToken)
    {
        var activeCredit = wallet.GetActiveCredit();
        if (activeCredit == null)
        {
            return new IntegratedPurchaseResult
            {
                IsSuccessful = false,
                ErrorMessage = "اعتبار فعالی برای این کاربر وجود ندارد"
            };
        }

        if (!activeCredit.CanUseCredit(purchaseAmount))
        {
            return new IntegratedPurchaseResult
            {
                IsSuccessful = false,
                ErrorMessage = $"اعتبار کافی نیست. اعتبار موجود: {activeCredit.AvailableCredit.Value:N0}"
            };
        }

        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // Use credit
            activeCredit.UseCredit(purchaseAmount);

            // Get default account (IRR) for transaction recording
            var defaultAccount = wallet.GetDefaultAccount();
            if (defaultAccount == null)
            {
                defaultAccount = wallet.CreateCurrencyAccount(CurrencyCode.IRR);
            }

            // Create credit transaction
            var creditTransaction = Transaction.CreateCreditTransaction(
                wallet.Id,
                defaultAccount.Id,
                wallet.UserId,
                purchaseAmount,
                $"خرید اعتباری - {request.Description}",
                activeCredit.DueDate,
                request.OrderId);

            // Mark as completed immediately (credit transactions are instant)
            creditTransaction.MarkAsCompleted();

            await unitOfWork.Transactions.AddAsync(creditTransaction, ct);
            await unitOfWork.SaveChangesAsync(ct);

            return new IntegratedPurchaseResult
            {
                IsSuccessful = true,
                PurchaseType = PurchaseType.Credit,
                TotalAmount = request.TotalAmount,
                WalletBalance = defaultAccount.Balance.Value,
                RequiredPayment = 0,
                PurchaseTransactionId = creditTransaction.Id,
                ProcessedAt = DateTime.UtcNow
            };
        }, cancellationToken);
    }
}