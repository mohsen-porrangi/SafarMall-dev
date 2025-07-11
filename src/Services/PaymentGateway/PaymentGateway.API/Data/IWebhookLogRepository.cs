using BuildingBlocks.Contracts;
using PaymentGateway.API.Models;

namespace PaymentGateway.API.Data;

/// <summary>
/// رابط Repository برای WebhookLog
/// </summary>
public interface IWebhookLogRepository : IRepositoryBase<WebhookLog, Guid>
{
    /// <summary>
    /// دریافت لاگ‌های پردازش نشده
    /// </summary>
    Task<IEnumerable<WebhookLog>> GetUnprocessedLogsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// حذف لاگ‌های قدیمی
    /// </summary>
    Task DeleteOldLogsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// دریافت لاگ‌ها بر اساس PaymentId
    /// </summary>
    Task<IEnumerable<WebhookLog>> GetByPaymentIdAsync(string paymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// دریافت webhook های پردازش نشده (alias method برای سازگاری)
    /// </summary>
    Task<IEnumerable<WebhookLog>> GetUnprocessedWebhooksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// دریافت webhook ها بر اساس PaymentId (alias method برای سازگاری)
    /// </summary>
    Task<IEnumerable<WebhookLog>> GetWebhooksByPaymentIdAsync(string paymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// پاکسازی لاگ‌های قدیمی (alias method برای سازگاری)
    /// </summary>
    Task CleanupOldLogsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
}