using BuildingBlocks.Contracts;
using PaymentGateway.API.Models;

namespace PaymentGateway.API.Data;

/// <summary>
/// رابط مخزن پرداخت
/// </summary>
public interface IPaymentRepository : IRepositoryBase<Payment, Guid>
{
    /// <summary>
    /// دریافت پرداخت با شناسه پرداخت
    /// </summary>
    Task<Payment?> GetByPaymentIdAsync(string paymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// دریافت پرداخت با شناسه مرجع درگاه
    /// </summary>
    Task<Payment?> GetByGatewayReferenceAsync(string gatewayReference, CancellationToken cancellationToken = default);

    /// <summary>
    /// دریافت پرداخت‌های منقضی شده
    /// </summary>
    Task<IEnumerable<Payment>> GetExpiredPaymentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// دریافت پرداخت‌های در انتظار بررسی مجدد
    /// </summary>
    Task<IEnumerable<Payment>> GetPendingRetryPaymentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// دریافت آمار پرداخت‌ها
    /// </summary>
    Task<PaymentStatistics> GetPaymentStatisticsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// آمار پرداخت‌ها
/// </summary>
public record PaymentStatistics
{
    public int TotalPayments { get; init; }
    public int SuccessfulPayments { get; init; }
    public int FailedPayments { get; init; }
    public int PendingPayments { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal SuccessfulAmount { get; init; }
}