using BuildingBlocks.Contracts;
using BuildingBlocks.CQRS;
using BuildingBlocks.Enums;
using BuildingBlocks.Messaging.Contracts;
using BuildingBlocks.Messaging.Events.PaymentEvents;
using PaymentGateway.API.Common;
using PaymentGateway.API.Common.Contracts;
using PaymentGateway.API.Data;
using PaymentGateway.API.Models;

namespace PaymentGateway.API.Features.Command.CreatePayment;

/// <summary>
/// پردازشگر دستور ایجاد پرداخت
/// Updated to store UserId for wallet integration
/// </summary>
public class CreatePaymentHandler(
    IPaymentGatewayFactory gatewayFactory,
    IUnitOfWork unitOfWork,
    ILogger<CreatePaymentHandler> logger,
    ICurrentUserService userService,
    IMessageBus messageBus
    ) : ICommandHandler<CreatePaymentCommand, CreatePaymentResponse>
{    
    public async Task<CreatePaymentResponse> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId();
        try
        {            
            // تولید شناسه یکتای پرداخت
            var paymentId = GeneratePaymentId();
       

            // دریافت ارائه‌دهنده درگاه
            var provider = gatewayFactory.GetProvider(request.PaymentGateway);

            logger.LogInformation("Creating payment {PaymentId} via {GatewayType} for amount {Amount}, UserId: {UserId}",
                paymentId, request.PaymentGateway, request.Amount, userId);

            // ایجاد پرداخت در درگاه
            var result = await provider.CreatePaymentAsync(
                request.Amount,
                request.Description,                
                request.OrderId,
                cancellationToken);

            // ایجاد رکورد پرداخت
            var payment = new Payment
            {
                PaymentId = paymentId,
                GatewayType = request.PaymentGateway,
                Currency = request.Currency,
                UserId = userId,
                Amount = request.Amount,
                Description = request.Description,
                CallbackUrl = result.CallbackUrl,
                OrderId = request.OrderId,
                Status = PaymentStatus.Pending,
                ExpiresAt = DateTime.UtcNow.AddMinutes(BusinessRules.Payment.ExpirationMinutes)
            };

            if (result.IsSuccessful)
            {
                // بروزرسانی رکورد با اطلاعات درگاه
                payment.GatewayReference = result.GatewayReference!;

                // ذخیره در دیتابیس
                await unitOfWork.Payments.AddAsync(payment, cancellationToken);            
                await unitOfWork.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Payment created successfully: PaymentId: {PaymentId}, GatewayReference: {GatewayReference}, UserId: {UserId}",
                    paymentId, result.GatewayReference, userId);

                return new CreatePaymentResponse
                {
                    IsSuccessful = true,
                    PaymentId = paymentId,
                    PaymentUrl = result.PaymentUrl,
                    GatewayReference = result.GatewayReference,
                    ExpiresAt = payment.ExpiresAt
                };
            }

            // در صورت خطا، پرداخت را به عنوان ناموفق علامت‌گذاری
            payment.MarkAsFailed(result.ErrorMessage, result.ErrorCode);
            await unitOfWork.Payments.AddAsync(payment, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogWarning(
                "Payment creation failed: PaymentId: {PaymentId}, UserId: {UserId}, Error: {Error}",
                paymentId, userId, result.ErrorMessage);

            return new CreatePaymentResponse
            {
                IsSuccessful = false,
                PaymentId = paymentId,
                ErrorMessage = result.ErrorMessage,
                ErrorCode = result.ErrorCode
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating payment for amount {Amount} via {GatewayType}, UserId: {UserId}",
                request.Amount, request.PaymentGateway, userId);

            return new CreatePaymentResponse
            {
                IsSuccessful = false,
                ErrorMessage = "خطا در ایجاد پرداخت",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    /// <summary>
    /// تولید شناسه یکتای پرداخت
    /// فرمت: PAY-YYYYMMDD-HHMMSS-XXXX
    /// </summary>
    private static string GeneratePaymentId()
    {
        var now = DateTime.UtcNow;
        var dateStr = now.ToString("yyyyMMdd");
        var timeStr = now.ToString("HHmmss");
        var randomPart = Random.Shared.Next(1000, 9999);

        return $"PAY-{dateStr}-{timeStr}-{randomPart}";
    }
}