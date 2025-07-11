using BuildingBlocks.Data;
using BuildingBlocks.Enums;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.API.Models;

namespace PaymentGateway.API.Data;

/// <summary>
/// پیاده‌سازی مخزن پرداخت
/// </summary>
public class PaymentRepository : RepositoryBase<Payment, Guid, PaymentDbContext>, IPaymentRepository
{
    public PaymentRepository(PaymentDbContext context) : base(context)
    {
    }

    public async Task<Payment?> GetByPaymentIdAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId && !p.IsDeleted, cancellationToken);
    }

    public async Task<Payment?> GetByGatewayReferenceAsync(string gatewayReference, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(p => p.GatewayReference == gatewayReference && !p.IsDeleted, cancellationToken);
    }

    public async Task<IEnumerable<Payment>> GetExpiredPaymentsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await DbSet
            .Where(p => p.Status == PaymentStatus.Pending &&
                       p.ExpiresAt < now &&
                       !p.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Payment>> GetPendingRetryPaymentsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.Status == PaymentStatus.Pending &&
                       p.RetryCount < p.MaxRetries &&
                       !p.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentStatistics> GetPaymentStatisticsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(p => !p.IsDeleted);

        if (fromDate.HasValue)
            query = query.Where(p => p.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(p => p.CreatedAt <= toDate.Value);

        var stats = await query
            .GroupBy(p => 1)
            .Select(g => new PaymentStatistics
            {
                TotalPayments = g.Count(),
                SuccessfulPayments = g.Count(p => p.Status == PaymentStatus.Paid),
                FailedPayments = g.Count(p => p.Status == PaymentStatus.Failed),
                PendingPayments = g.Count(p => p.Status == PaymentStatus.Pending),
                TotalAmount = g.Sum(p => p.Amount),
                SuccessfulAmount = g.Where(p => p.Status == PaymentStatus.Paid).Sum(p => p.Amount)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return stats ?? new PaymentStatistics();
    }
}