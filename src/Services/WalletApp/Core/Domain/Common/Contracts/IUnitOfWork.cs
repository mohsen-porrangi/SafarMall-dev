using Microsoft.EntityFrameworkCore.Storage;

namespace WalletApp.Domain.Common.Contracts;

/// <summary>
/// Unit of Work pattern for wallet domain
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Wallet repository
    /// </summary>
    IWalletRepository Wallets { get; }

    /// <summary>
    /// Transaction repository  
    /// </summary>
    ITransactionRepository Transactions { get; }
  //  ICreditsRepository Credits { get; }

    /// <summary>
    /// Save all changes asynchronously
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begin database transaction
    /// </summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commit current transaction
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback current transaction
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute in transaction scope
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute in transaction scope without return value
    /// </summary>
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}