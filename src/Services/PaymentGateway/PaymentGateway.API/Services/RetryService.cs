using BuildingBlocks.Enums;
using PaymentGateway.API.Common.Contracts;
using PaymentGateway.API.Data;
using PaymentGateway.API.Models;
using Polly;
using Polly.Retry;

namespace PaymentGateway.API.Services;

/// <summary>
/// سرویس تلاش مجدد
/// </summary>
public interface IRetryService
{
    /// <summary>
    /// تلاش مجدد برای تایید پرداخت
    /// </summary>
    Task<bool> RetryPaymentVerificationAsync(
        string paymentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// تلاش مجدد برای پرداخت‌های معلق
    /// </summary>
    Task ProcessPendingPaymentsAsync(CancellationToken cancellationToken = default);
}

public class RetryService : IRetryService
{
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RetryService> _logger;

    // Policy برای تلاش مجدد
    private readonly AsyncRetryPolicy _retryPolicy;

    public RetryService(
        IPaymentGatewayFactory gatewayFactory,
        IUnitOfWork unitOfWork,
        ILogger<RetryService> logger)
    {
        _gatewayFactory = gatewayFactory;
        _unitOfWork = unitOfWork;
        _logger = logger;

        // تنظیم Polly برای Exponential Backoff
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Retry attempt {RetryCount} after {Delay}ms",
                        retryCount, timespan.TotalMilliseconds);
                });
    }

    public async Task<bool> RetryPaymentVerificationAsync(
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _unitOfWork.Payments.FirstOrDefaultAsync(
                p => p.PaymentId == paymentId && !p.IsDeleted,
                cancellationToken: cancellationToken);

            if (payment == null)
            {
                _logger.LogWarning("Payment {PaymentId} not found for retry", paymentId);
                return false;
            }

            if (!payment.CanRetry)
            {
                _logger.LogInformation("Payment {PaymentId} cannot be retried", paymentId);
                return false;
            }

            var provider = _gatewayFactory.GetProvider(payment.GatewayType);

            // تلاش با Polly
            var result = await _retryPolicy.ExecuteAsync(async () =>
            {
                return await provider.VerifyPaymentAsync(
                    payment.GatewayReference,
                    payment.Amount,
                    cancellationToken);
            });

            // افزایش تعداد تلاش
            payment.IncrementRetry();

            if (result.IsSuccessful && result.IsVerified)
            {
                payment.MarkAsPaid(result.TransactionId!, result.TrackingCode);
                _logger.LogInformation("Payment {PaymentId} verified successfully on retry", paymentId);
            }
            else
            {
                payment.MarkAsFailed(result.ErrorMessage, result.ErrorCode);
                _logger.LogWarning("Payment {PaymentId} verification failed on retry: {Error}",
                    paymentId, result.ErrorMessage);
            }

            _unitOfWork.Payments.Update(payment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return result.IsSuccessful && result.IsVerified;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during retry verification for payment {PaymentId}", paymentId);
            return false;
        }
    }

    public async Task ProcessPendingPaymentsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // دریافت پرداخت‌های معلق که قابل تلاش مجدد هستند
            var pendingPayments = await _unitOfWork.Payments.FindAsync(
                p => p.Status == PaymentStatus.Pending &&
                     p.RetryCount < p.MaxRetries &&
                     p.CreatedAt > DateTime.UtcNow.AddHours(-24), // فقط 24 ساعت گذشته
                cancellationToken: cancellationToken);

            foreach (var payment in pendingPayments)
            {
                // تاخیر بین پردازش پرداخت‌ها
                await Task.Delay(1000, cancellationToken);

                await RetryPaymentVerificationAsync(payment.PaymentId, cancellationToken);
            }

            _logger.LogInformation("Processed {Count} pending payments", pendingPayments.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending payments");
        }
    }
}