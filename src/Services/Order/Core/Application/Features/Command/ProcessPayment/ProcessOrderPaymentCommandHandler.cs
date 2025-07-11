using BuildingBlocks.Contracts;
using BuildingBlocks.CQRS;
using BuildingBlocks.Enums;
using BuildingBlocks.Exceptions;
using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces;
using Order.Domain.Contracts;
using Order.Domain.Enums;

namespace Order.Application.Features.Command.ProcessPayment;

/// <summary>
/// Handler for processing order payment through Wallet Service
/// Orchestrates: Order → Wallet → Train Service notification
/// </summary>
public class ProcessOrderPaymentCommandHandler(
    IUnitOfWork unitOfWork,
    IWalletServiceClient walletService,
    ICurrentUserService currentUserService,
    ILogger<ProcessOrderPaymentCommandHandler> logger
) : ICommandHandler<ProcessOrderPaymentCommand, ProcessOrderPaymentResult>
{
    public async Task<ProcessOrderPaymentResult> Handle(
        ProcessOrderPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.GetCurrentUserId();

        logger.LogInformation("Processing payment for order {OrderId} by user {UserId}",
            request.OrderId, currentUserId);

        try
        {
            // Step 1: Get and validate order
            var order = await GetAndValidateOrderAsync(request.OrderId, currentUserId, cancellationToken);

            // Step 2: Process payment through Wallet Service
            var walletRequest = new IntegratedPurchaseRequest
            {
                UserId = currentUserId,
                TotalAmount = order.TotalAmount,
                Currency = BuildingBlocks.Enums.CurrencyCode.IRR,
                PaymentGateway = request.PaymentGateway,
                OrderId = order.Id,
                Description = $"پرداخت سفارش {order.ServiceType.ToString()} - {order.OrderNumber}",
                UseCredit = request.UseCredit
            };

            var paymentResult = await walletService.IntegratedPurchaseAsync(walletRequest, cancellationToken);

            if (!paymentResult.IsSuccessful)
            {
                logger.LogWarning("Wallet operation failed for order {OrderId}: {Error}",
                    order.Id, paymentResult.ErrorMessage);

                return new ProcessOrderPaymentResult
                {
                    IsSuccessful = false,
                    ErrorMessage = paymentResult.ErrorMessage ?? "خطا در پردازش پرداخت از کیف پول",
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber
                };
            }
            // Additional validation for FullWallet payments
            if (paymentResult.WalletPurchaseType == PurchaseType.FullWallet)
            {
                if (paymentResult.PurchaseTransactionId == null)
                {
                    logger.LogError("FullWallet payment succeeded but no transaction ID returned for order {OrderId}",
                        order.Id);

                    return new ProcessOrderPaymentResult
                    {
                        IsSuccessful = false,
                        ErrorMessage = "خطا در ثبت تراکنش کیف پول",
                        OrderId = order.Id,
                        OrderNumber = order.OrderNumber
                    };
                }
            }

            logger.LogInformation("Payment processed successfully for order {OrderId}, PaymentType: {PaymentType}",
                order.Id, paymentResult.WalletPurchaseType);

            // Step 3: Update order based on payment type
            await UpdateOrderForPaymentAsync(order, paymentResult, cancellationToken);

            // Step 4: Handle different payment scenarios
            var result = await HandlePaymentScenarioAsync(order, paymentResult, cancellationToken);

            logger.LogInformation("Payment processed successfully for order {OrderId}, PaymentType: {PaymentType}",
                request.OrderId, paymentResult.WalletPurchaseType);

            return result;
        }
        catch (Exception ex) when (ex is not NotFoundException and not BadRequestException and not ForbiddenDomainException)
        {
            logger.LogError(ex, "Unexpected error processing payment for order {OrderId}", request.OrderId);
            throw new InternalServerException("خطای سیستمی در پردازش پرداخت");
        }
    }

    /// <summary>
    /// Get and validate order for payment processing
    /// </summary>
    private async Task<Domain.Entities.Order> GetAndValidateOrderAsync(
        Guid orderId,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var order = await unitOfWork.Orders.FirstOrDefaultAsync(x => x.Id.Equals(orderId), false, cancellationToken)
            ?? throw new NotFoundException("سفارش یافت نشد");

        // Validate ownership
        if (order.UserId != currentUserId)
            throw new ForbiddenDomainException("شما اجازه پرداخت این سفارش را ندارید");

        // Validate order status
        if (order.LastStatus != OrderStatus.Pending)
            throw new BadRequestException($"سفارش در وضعیت {order.LastStatus} قابل پرداخت نیست");

        // Validate amount
        if (order.TotalAmount <= 0)
            throw new BadRequestException("مبلغ سفارش نامعتبر است");

        return order;
    }

    /// <summary>
    /// Update order with payment information
    /// </summary>
    private async Task UpdateOrderForPaymentAsync(
        Domain.Entities.Order order,
        IntegratedPurchaseResult paymentResult,
        CancellationToken cancellationToken)
    {
        // Set payment reference (transaction ID or authority)
        var paymentReference = paymentResult.PurchaseTransactionId?.ToString()
                              ?? paymentResult.Authority
                              ?? paymentResult.PaymentTransactionId?.ToString();

        order.SetPaymentReference(paymentReference);

        // Add wallet transaction if wallet was used
        if (paymentResult.PurchaseTransactionId.HasValue)
        {
            order.AddWalletTransaction(
                paymentResult.PurchaseTransactionId.Value.GetHashCode(), // Convert Guid to long
                OrderTransactionType.Purchase,
                paymentResult.TotalAmount - paymentResult.RequiredPayment); // Wallet portion
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Handle different payment scenarios
    /// </summary>
    private async Task<ProcessOrderPaymentResult> HandlePaymentScenarioAsync(
        Domain.Entities.Order order,
        IntegratedPurchaseResult paymentResult,
        CancellationToken cancellationToken)
    {
        return paymentResult.WalletPurchaseType switch
        {
            PurchaseType.FullWallet => await HandleFullWalletPaymentAsync(order, paymentResult, cancellationToken),
            PurchaseType.FullPayment => HandleFullGatewayPayment(order, paymentResult),
            PurchaseType.Mixed => HandleMixedPayment(order, paymentResult),
            PurchaseType.Credit => await HandleCreditPaymentAsync(order, paymentResult, cancellationToken),
            _ => throw new InvalidOperationException($"Unknown payment type: {paymentResult.WalletPurchaseType}")
        };
    }

    /// <summary>
    /// Handle full wallet payment (immediate completion)
    /// </summary>
    private async Task<ProcessOrderPaymentResult> HandleFullWalletPaymentAsync(
        Domain.Entities.Order order,
        IntegratedPurchaseResult paymentResult,
        CancellationToken cancellationToken)
    {
        // Mark order as paid immediately
        order.MarkAsPaid();
        order.UpdateStatus(OrderStatus.Processing, "پرداخت از کیف پول با موفقیت انجام شد");

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // TODO: Notify Train Service to complete reservation
        // This would be implemented when we have Train Service notification
        // await NotifyTrainServiceAsync(order, cancellationToken);

        return CreateSuccessResult(order, paymentResult);
    }

    /// <summary>
    /// Handle full gateway payment (redirect to gateway)
    /// </summary>
    private ProcessOrderPaymentResult HandleFullGatewayPayment(
        Domain.Entities.Order order,
        IntegratedPurchaseResult paymentResult)
    {
        // Order stays in Pending status until gateway callback
        order.UpdateStatus(OrderStatus.PendingPayment, "انتظار برای تکمیل پرداخت در درگاه");

        return CreateSuccessResult(order, paymentResult);
    }

    /// <summary>
    /// Handle mixed payment (partial wallet + gateway)
    /// </summary>
    private ProcessOrderPaymentResult HandleMixedPayment(
        Domain.Entities.Order order,
        IntegratedPurchaseResult paymentResult)
    {
        // Wallet portion is deducted, waiting for gateway completion
        order.UpdateStatus(OrderStatus.PendingPayment, "بخشی از مبلغ از کیف پول کسر شد، انتظار برای تکمیل پرداخت در درگاه");

        return CreateSuccessResult(order, paymentResult);
    }

    /// <summary>
    /// Handle credit payment (B2B - immediate completion)
    /// </summary>
    private async Task<ProcessOrderPaymentResult> HandleCreditPaymentAsync(
        Domain.Entities.Order order,
        IntegratedPurchaseResult paymentResult,
        CancellationToken cancellationToken)
    {
        // Credit payments are completed immediately
        order.MarkAsPaid();
        order.UpdateStatus(OrderStatus.Processing, "پرداخت اعتباری با موفقیت انجام شد");

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // TODO: Notify Train Service to complete reservation
        // await NotifyTrainServiceAsync(order, cancellationToken);

        return CreateSuccessResult(order, paymentResult);
    }

    /// <summary>
    /// Create success result
    /// </summary>
    private static ProcessOrderPaymentResult CreateSuccessResult(
        Domain.Entities.Order order,
        IntegratedPurchaseResult paymentResult)
    {
        return new ProcessOrderPaymentResult
        {
            IsSuccessful = true,
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            PaymentType = paymentResult.WalletPurchaseType,
            TotalAmount = paymentResult.TotalAmount,
            WalletBalance = paymentResult.WalletBalance,
            RequiredPayment = paymentResult.RequiredPayment,
            PurchaseTransactionId = paymentResult.PurchaseTransactionId,
            PaymentTransactionId = paymentResult.PaymentTransactionId,
            PaymentUrl = paymentResult.PaymentUrl,
            Authority = paymentResult.Authority,
            ProcessedAt = paymentResult.ProcessedAt
        };
    }


    // TODO: Implement Train Service notification
    // private async Task NotifyTrainServiceAsync(Domain.Entities.Order order, CancellationToken cancellationToken)
    // {
    //     // This will be implemented when we have Train Service HTTP client in Order Service
    //     // or when we implement event-based notification
    // }
}