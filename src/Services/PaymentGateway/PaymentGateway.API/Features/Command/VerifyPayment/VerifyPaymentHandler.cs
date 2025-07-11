using BuildingBlocks.CQRS;
using BuildingBlocks.Enums;
using BuildingBlocks.Messaging.Contracts;
using BuildingBlocks.Messaging.Events.PaymentEvents;
using PaymentGateway.API.Common.Contracts;
using PaymentGateway.API.Data;
using PaymentGateway.API.Models;
using PaymentGateway.API.Services;

namespace PaymentGateway.API.Features.Command.VerifyPayment;

/// <summary>
/// پردازش‌کننده تایید پرداخت
/// Updated with event publishing for wallet charging
/// </summary>
public class VerifyPaymentHandler(
    IUnitOfWork unitOfWork,
    IPaymentGatewayFactory gatewayFactory,
    IMessageBus messageBus,
    IWalletServiceClient walletServiceClient,
    ILogger<VerifyPaymentHandler> logger
    ) : ICommandHandler<VerifyPaymentCommand, VerifyPaymentResponse>
{ 

    public async Task<VerifyPaymentResponse> Handle(
       VerifyPaymentCommand request,
       CancellationToken cancellationToken)
    {
        try
        {
            // یافتن پرداخت بر اساس GatewayReference
            var payment = await unitOfWork.Payments.GetByGatewayReferenceAsync(
                request.GatewayReference, cancellationToken);

            if (payment == null)
            {
                return new VerifyPaymentResponse
                {
                    IsSuccessful = false,
                    IsVerified = false,
                    Status = PaymentStatus.Failed,
                    ErrorMessage = "پرداخت یافت نشد",
                    ErrorCode = "PAYMENT_NOT_FOUND"
                };
            }

            // بررسی وضعیت قبلی
            if (payment.Status == PaymentStatus.Paid)
            {
                return new VerifyPaymentResponse
                {
                    IsSuccessful = true,
                    IsVerified = true,
                    PaymentId = payment.PaymentId,
                    TransactionId = payment.TransactionId,
                    TrackingCode = payment.TrackingCode,
                    Amount = payment.Amount,
                    Status = payment.Status,
                    VerificationDate = payment.PaidAt
                };
            }

            // بررسی انقضا
            if (payment.IsExpired)
            {
                payment.Status = PaymentStatus.Expired;
                await unitOfWork.SaveChangesAsync(cancellationToken);

                return new VerifyPaymentResponse
                {
                    IsSuccessful = false,
                    IsVerified = false,
                    PaymentId = payment.PaymentId,
                    Status = PaymentStatus.Expired,
                    ErrorMessage = "پرداخت منقضی شده است",
                    ErrorCode = "PAYMENT_EXPIRED"
                };
            }

            // بررسی وضعیت بازگشتی
            if (request.Status != "OK")
            {
                payment.MarkAsFailed($"Payment cancelled with status: {request.Status}");
                payment.Status = PaymentStatus.Cancelled;
                await unitOfWork.SaveChangesAsync(cancellationToken);

                return new VerifyPaymentResponse
                {
                    IsSuccessful = false,
                    IsVerified = false,
                    PaymentId = payment.PaymentId,
                    Status = PaymentStatus.Cancelled,
                    ErrorMessage = "پرداخت لغو شد",
                    ErrorCode = "PAYMENT_CANCELLED"
                };
            }

            // تایید با درگاه
            var provider = gatewayFactory.GetProvider(payment.GatewayType);
            var verifyResult = await provider.VerifyPaymentAsync(
                request.GatewayReference,
                payment.Amount,
                cancellationToken);

            if (verifyResult.IsSuccessful && verifyResult.IsVerified)
            {
                // اعتبارسنجی مبلغ
                if (request.Amount.HasValue &&
                    Math.Abs(payment.Amount - request.Amount.Value) > 0.01m)
                {
                    payment.MarkAsFailed(
                        $"Amount mismatch: expected {payment.Amount}, received {request.Amount}");
                    await unitOfWork.SaveChangesAsync(cancellationToken);

                    return new VerifyPaymentResponse
                    {
                        IsSuccessful = false,
                        IsVerified = false,
                        PaymentId = payment.PaymentId,
                        Status = PaymentStatus.Failed,
                        ErrorMessage = "مبلغ پرداخت مطابقت ندارد",
                        ErrorCode = "AMOUNT_MISMATCH"
                    };
                }

                // علامت‌گذاری به عنوان موفق
                payment.MarkAsPaid(verifyResult.TransactionId!, verifyResult.TrackingCode);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                // CRITICAL: Direct HTTP to Wallet Service
                if (payment.UserId.HasValue)
                {
                    var callbackRequest = new PaymentCallbackRequest(
                        request.GatewayReference,
                        "OK",
                        payment.Amount,
                        verifyResult.TransactionId!,
                        verifyResult.TrackingCode);

                    var walletNotified = await walletServiceClient.ProcessPaymentCallbackAsync(
                        callbackRequest, cancellationToken);

                    if (!walletNotified)
                    {
                        logger.LogWarning("Failed to notify Wallet Service for PaymentId: {PaymentId}",
                            payment.PaymentId);
                    }
                }

                // Emit event for wallet charging - KEY ADDITION
                if (payment.UserId.HasValue)
                {
                    var paymentVerifiedEvent = new PaymentVerifiedEvent(
                        payment.PaymentId,
                        payment.GatewayReference,
                        payment.UserId.Value, // اضافه شده
                        payment.Amount,
                        verifyResult.TransactionId!,
                        verifyResult.TrackingCode,
                        payment.OrderId);

                    await messageBus.PublishAsync(paymentVerifiedEvent, cancellationToken);

                    logger.LogInformation(
                        "PaymentVerifiedEvent published for PaymentId: {PaymentId}, UserId: {UserId}",
                        payment.PaymentId, payment.UserId.Value);
                }
                else
                {
                    logger.LogWarning(
                        "PaymentVerifiedEvent not published - UserId is null for PaymentId: {PaymentId}",
                        payment.PaymentId);
                }

                return new VerifyPaymentResponse
                {
                    IsSuccessful = true,
                    IsVerified = true,
                    PaymentId = payment.PaymentId,
                    TransactionId = verifyResult.TransactionId,
                    TrackingCode = verifyResult.TrackingCode,
                    Amount = payment.Amount,
                    Status = PaymentStatus.Paid,
                    VerificationDate = DateTime.UtcNow
                };
            }
            else
            {
                // شکست در تایید
                payment.MarkAsFailed(verifyResult.ErrorMessage, verifyResult.ErrorCode);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                return new VerifyPaymentResponse
                {
                    IsSuccessful = false,
                    IsVerified = false,
                    PaymentId = payment.PaymentId,
                    Status = PaymentStatus.Failed,
                    ErrorMessage = verifyResult.ErrorMessage ?? "تایید پرداخت ناموفق بود",
                    ErrorCode = verifyResult.ErrorCode
                };
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during payment verification for GatewayReference: {GatewayReference}",
                request.GatewayReference);

            return new VerifyPaymentResponse
            {
                IsSuccessful = false,
                IsVerified = false,
                Status = PaymentStatus.Failed,
                ErrorMessage = "خطا در تایید پرداخت",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }
}