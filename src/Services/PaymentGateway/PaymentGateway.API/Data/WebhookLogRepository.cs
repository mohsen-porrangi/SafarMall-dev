using BuildingBlocks.Data;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.API.Models;

namespace PaymentGateway.API.Data;

/// <summary>
/// پیاده‌سازی Repository برای WebhookLog
/// </summary>
public class WebhookLogRepository : RepositoryBase<WebhookLog, Guid, PaymentDbContext>, IWebhookLogRepository
{
    public WebhookLogRepository(PaymentDbContext context) : base(context)
    {
    }

    /// <summary>
    /// دریافت لاگ‌های پردازش نشده
    /// </summary>
    public async Task<IEnumerable<WebhookLog>> GetUnprocessedLogsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(wl => !wl.IsProcessed)
            .OrderBy(wl => wl.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// حذف لاگ‌های قدیمی
    /// </summary>
    public async Task DeleteOldLogsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        var oldLogs = await DbSet
            .Where(wl => wl.CreatedAt < cutoffDate)
            .ToListAsync(cancellationToken);

        if (oldLogs.Any())
        {
            DeleteRange(oldLogs);
        }
    }

    /// <summary>
    /// دریافت لاگ‌ها بر اساس PaymentId
    /// </summary>
    public async Task<IEnumerable<WebhookLog>> GetByPaymentIdAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(wl => wl.PaymentId == paymentId)
            .OrderByDescending(wl => wl.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// دریافت webhook های پردازش نشده (alias method)
    /// </summary>
    public async Task<IEnumerable<WebhookLog>> GetUnprocessedWebhooksAsync(CancellationToken cancellationToken = default)
    {
        return await GetUnprocessedLogsAsync(cancellationToken);
    }

    /// <summary>
    /// دریافت webhook ها بر اساس PaymentId (alias method)
    /// </summary>
    public async Task<IEnumerable<WebhookLog>> GetWebhooksByPaymentIdAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        return await GetByPaymentIdAsync(paymentId, cancellationToken);
    }

    /// <summary>
    /// پاکسازی لاگ‌های قدیمی (alias method)
    /// </summary>
    public async Task CleanupOldLogsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        await DeleteOldLogsAsync(cutoffDate, cancellationToken);
    }
}