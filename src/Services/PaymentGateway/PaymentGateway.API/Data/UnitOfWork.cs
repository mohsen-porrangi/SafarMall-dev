using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace PaymentGateway.API.Data;

/// <summary>
/// پیاده‌سازی Unit of Work برای Payment Gateway
/// </summary>
public class UnitOfWork(
    ILogger<UnitOfWork> logger,
    PaymentDbContext context,
    IPaymentRepository paymentRepository,
    IWebhookLogRepository? webhookLogRepository) : IUnitOfWork
{
    private IDbContextTransaction? _currentTransaction;

    // Lazy-loaded repositories
    public IPaymentRepository Payments { get; } = paymentRepository;
    public IWebhookLogRepository WebhookLogs { get; } = webhookLogRepository; 

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SaveChangesAsync");
            throw;
        }
        
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
            throw new InvalidOperationException("Transaction already in progress");

        _currentTransaction = await context.Database.BeginTransactionAsync(cancellationToken);
        return _currentTransaction;
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
            throw new InvalidOperationException("No transaction in progress");

        try
        {
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
            throw new InvalidOperationException("No transaction in progress");

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            // Already in transaction, just execute
            return await operation(cancellationToken);
        }

        using var transaction = await BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            await CommitTransactionAsync(cancellationToken);
            return result;
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync(async ct =>
        {
            await operation(ct);
            return true;
        }, cancellationToken);
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        context.Dispose();
    }
}
