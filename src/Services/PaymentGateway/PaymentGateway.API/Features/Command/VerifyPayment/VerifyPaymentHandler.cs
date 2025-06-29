using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Contracts;
using BuildingBlocks.Messaging.Events.PaymentEvents;
using PaymentGateway.API.Common.Contracts;
using PaymentGateway.API.Data;
using PaymentGateway.API.Models;

namespace PaymentGateway.API.Features.Command.VerifyPayment;

/// <summary>
/// پردازش‌کننده تایید پرداخت
/// Updated with event publishing for wallet charging
/// </summary>
public class VerifyPaymentHandler : ICommandHandler<VerifyPaymentCommand, VerifyPaymentResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<VerifyPaymentHandler> _logger;

    public VerifyPaymentHandler(
        IUnitOfWork unitOfWork,
        IPaymentGatewayFactory gatewayFactory,
        IMessageBus messageBus,
        ILogger<VerifyPaymentHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _gatewayFactory = gatewayFactory ?? throw new ArgumentNullException(nameof(gatewayFactory));
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<VerifyPaymentResponse> Handle(
       VerifyPaymentCommand request,
       CancellationToken cancellationToken)
    {
        try
        {
            // یافتن پرداخت بر اساس GatewayReference
            var payment = await _unitOfWork.Payments.GetByGatewayReferenceAsync(
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
                await _unitOfWork.SaveChangesAsync(cancellationToken);

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
                await _unitOfWork.SaveChangesAsync(cancellationToken);

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
            var provider = _gatewayFactory.GetProvider(payment.GatewayType);
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
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

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
                await _unitOfWork.SaveChangesAsync(cancellationToken);

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

                    await _messageBus.PublishAsync(paymentVerifiedEvent, cancellationToken);

                    _logger.LogInformation(
                        "PaymentVerifiedEvent published for PaymentId: {PaymentId}, UserId: {UserId}",
                        payment.PaymentId, payment.UserId.Value);
                }
                else
                {
                    _logger.LogWarning(
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
                await _unitOfWork.SaveChangesAsync(cancellationToken);

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
            _logger.LogError(ex, "Error during payment verification for GatewayReference: {GatewayReference}",
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