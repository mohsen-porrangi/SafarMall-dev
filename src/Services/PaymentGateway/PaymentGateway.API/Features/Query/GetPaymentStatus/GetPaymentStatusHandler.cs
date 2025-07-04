using BuildingBlocks.CQRS;
using BuildingBlocks.Enums;
using BuildingBlocks.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using PaymentGateway.API.Common.Contracts;
using PaymentGateway.API.Data;

namespace PaymentGateway.API.Features.Query.GetPaymentStatus;

/// <summary>
/// پردازشگر دریافت وضعیت پرداخت
/// </summary>
public class GetPaymentStatusHandler(IUnitOfWork unitOfWork,
        IPaymentGatewayFactory gatewayFactory,
        IMemoryCache cache,
        ILogger<GetPaymentStatusHandler> logger) : IQueryHandler<GetPaymentStatusQuery, GetPaymentStatusResponse>
{
    public async Task<GetPaymentStatusResponse> Handle(
        GetPaymentStatusQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting payment status for PaymentId: {PaymentId}", request.PaymentId);

        // بررسی cache
        var cacheKey = $"payment_status_{request.PaymentId}";
        if (cache.TryGetValue(cacheKey, out GetPaymentStatusResponse? cachedResponse))
        {
            logger.LogDebug("Payment status retrieved from cache for PaymentId: {PaymentId}", request.PaymentId);
            return cachedResponse!;
        }

        // یافتن پرداخت از دیتابیس
        var payment = await unitOfWork.Payments.GetByPaymentIdAsync(request.PaymentId, cancellationToken);
        if (payment == null)
        {
            throw new NotFoundException("Payment not found", request.PaymentId);
        }

        // اگر پرداخت قبلاً تکمیل شده، از دیتابیس برگردان
        if (payment.Status == PaymentStatus.Paid ||
            payment.Status == PaymentStatus.Failed ||
            payment.Status == PaymentStatus.Cancelled)
        {
            var response = CreateResponse(payment);

            // Cache برای 15 دقیقه
            cache.Set(cacheKey, response, TimeSpan.FromMinutes(15));

            return response;
        }

        // بررسی انقضا
        if (payment.IsExpired)
        {
            payment.Status = PaymentStatus.Expired;
            payment.ErrorMessage = "Payment has expired";
            unitOfWork.Payments.Update(payment);

            return CreateResponse(payment);
        }

        // اگر پرداخت pending است، از درگاه بررسی کن
        try
        {
            var provider = gatewayFactory.GetProvider(payment.GatewayType);
            var statusResult = await provider.GetPaymentStatusAsync(
                payment.GatewayReference,
                cancellationToken);

            if (statusResult.IsSuccessful)
            {
                // بروزرسانی وضعیت در دیتابیس
                payment.Status = statusResult.Status;
                if (statusResult.Status == PaymentStatus.Paid)
                {
                    payment.MarkAsPaid(
                        statusResult.TransactionId ?? payment.GatewayReference,
                        statusResult.TrackingCode);
                }
                else if (statusResult.Status == PaymentStatus.Failed)
                {
                    payment.MarkAsFailed(statusResult.ErrorMessage);
                }

                unitOfWork.Payments.Update(payment);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get payment status from gateway for PaymentId: {PaymentId}",
                request.PaymentId);
            // ادامه با وضعیت موجود در دیتابیس
        }

        var finalResponse = CreateResponse(payment);

        // Cache فقط برای پرداخت‌های تکمیل شده
        if (payment.Status == PaymentStatus.Paid ||
            payment.Status == PaymentStatus.Failed ||
            payment.Status == PaymentStatus.Cancelled)
        {
            cache.Set(cacheKey, finalResponse, TimeSpan.FromMinutes(15));
        }

        return finalResponse;
    }

    private static GetPaymentStatusResponse CreateResponse(Payment payment)
    {
        return new GetPaymentStatusResponse
        {
            IsSuccessful = true,
            PaymentId = payment.PaymentId,
            GatewayType = payment.GatewayType,
            Status = payment.Status,
            Amount = payment.Amount,
            Description = payment.Description,
            TransactionId = payment.TransactionId,
            TrackingCode = payment.TrackingCode,
            ErrorMessage = payment.ErrorMessage,
            ErrorCode = payment.ErrorCode,
            CreatedAt = payment.CreatedAt,
            PaidAt = payment.PaidAt,
            ExpiresAt = payment.ExpiresAt,
            IsExpired = payment.IsExpired
        };
    }
}