using BuildingBlocks.CQRS;
using BuildingBlocks.Enums;
using PaymentGateway.API.Common;
using PaymentGateway.API.Common.Contracts;
using PaymentGateway.API.Data;
using PaymentGateway.API.Models;

namespace PaymentGateway.API.Features.Command.CreatePayment;

/// <summary>
/// پردازشگر دستور ایجاد پرداخت
/// Updated to store UserId for wallet integration
/// </summary>
public class CreatePaymentHandler : ICommandHandler<CreatePaymentCommand, CreatePaymentResponse>
{
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreatePaymentHandler> _logger;

    public CreatePaymentHandler(
        IPaymentGatewayFactory gatewayFactory,
        IUnitOfWork unitOfWork,
        ILogger<CreatePaymentHandler> logger)
    {
        _gatewayFactory = gatewayFactory ?? throw new ArgumentNullException(nameof(gatewayFactory));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CreatePaymentResponse> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // تولید شناسه یکتای پرداخت
            var paymentId = GeneratePaymentId();

            // ایجاد رکورد پرداخت
            var payment = new Payment
            {
                PaymentId = paymentId,
                GatewayType = request.GatewayType,
                UserId = request.UserId, // اضافه شده - ذخیره UserId
                Amount = request.Amount,
                Description = request.Description,
                CallbackUrl = request.CallbackUrl,
                OrderId = request.OrderId,
                Status = PaymentStatus.Pending,
                ExpiresAt = DateTime.UtcNow.AddMinutes(BusinessRules.Payment.ExpirationMinutes)
            };

            // دریافت ارائه‌دهنده درگاه
            var provider = _gatewayFactory.GetProvider(request.GatewayType);

            _logger.LogInformation("Creating payment {PaymentId} via {GatewayType} for amount {Amount}, UserId: {UserId}",
                paymentId, request.GatewayType, request.Amount, request.UserId);

            // ایجاد پرداخت در درگاه
            var result = await provider.CreatePaymentAsync(
                request.Amount,
                request.Description,
                request.CallbackUrl,
                request.OrderId,
                cancellationToken);

            if (result.IsSuccessful)
            {
                // بروزرسانی رکورد با اطلاعات درگاه
                payment.GatewayReference = result.GatewayReference!;

                // ذخیره در دیتابیس
                await _unitOfWork.Payments.AddAsync(payment, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Payment created successfully: PaymentId: {PaymentId}, GatewayReference: {GatewayReference}, UserId: {UserId}",
                    paymentId, result.GatewayReference, request.UserId);

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
            await _unitOfWork.Payments.AddAsync(payment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "Payment creation failed: PaymentId: {PaymentId}, UserId: {UserId}, Error: {Error}",
                paymentId, request.UserId, result.ErrorMessage);

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
            _logger.LogError(ex, "Error creating payment for amount {Amount} via {GatewayType}, UserId: {UserId}",
                request.Amount, request.GatewayType, request.UserId);

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