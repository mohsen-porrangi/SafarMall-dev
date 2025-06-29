using Microsoft.EntityFrameworkCore.Storage;

namespace PaymentGateway.API.Data;

/// <summary>
/// رابط Unit of Work برای Payment Gateway
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Repository پرداخت‌ها
    /// </summary>
    IPaymentRepository Payments { get; }

    /// <summary>
    /// Repository لاگ‌های Webhook
    /// </summary>
    IWebhookLogRepository WebhookLogs { get; }

    /// <summary>
    /// ذخیره تغییرات
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// شروع تراکنش دیتابیس
    /// </summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// تایید تراکنش
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// لغو تراکنش
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// اجرای عملیات در محدوده تراکنش
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// اجرای عملیات در محدوده تراکنش (بدون مقدار بازگشتی)
    /// </summary>
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}
