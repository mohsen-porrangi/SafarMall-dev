using BuildingBlocks.Contracts;
using DnsClient.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using WalletApp.Application.Common.Interfaces;
using WalletApp.Domain.Aggregates.WalletAggregate;
using WalletApp.Domain.Common.Contracts;
using WalletApp.Infrastructure.Persistence.Context;

namespace WalletApp.Infrastructure.Persistence;

/// <summary>
/// Unit of Work implementation for wallet domain
/// </summary>
public class UnitOfWork(
    WalletDbContext context,
    IWalletRepository walletRepository,
    ITransactionRepository transactionRepository,
    ILogger<UnitOfWork> logger,
    IDomainEventPublisher domainEventPublisher
    ) : IUnitOfWork, IDisposable
{
    private IDbContextTransaction? _currentTransaction;

    // Lazy-loaded repositories
    public IWalletRepository Wallets { get; } = walletRepository;
    public ITransactionRepository Transactions { get; } = transactionRepository;




    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            //1.Collect domain events before saving(important!)
            var domainEvents = CollectDomainEvents();
            // 3. Publish domain events after successful save
            if (domainEvents.Any())
            {
                logger.LogDebug("Publishing {Count} domain events after successful save", domainEvents.Count);
                await domainEventPublisher.PublishEventsAsync(domainEvents, cancellationToken);
            }


            // Deep debugging for BankAccount
            var bankAccountEntries = context.ChangeTracker.Entries<BankAccount>().ToList();

            foreach (var entry in bankAccountEntries)
            {
                var bankAccount = entry.Entity;
                logger.LogWarning($"=== BankAccount Debug ===");
                logger.LogWarning($"ID: {bankAccount.Id}");
                logger.LogWarning($"State: {entry.State}");
                logger.LogWarning($"CreatedAt: {bankAccount.CreatedAt}");
                logger.LogWarning($"UpdatedAt: {bankAccount.UpdatedAt}");
                logger.LogWarning($"IsKeySet: {entry.IsKeySet}");

                // Check original values
                if (entry.State == EntityState.Modified)
                {
                    var originalValues = entry.OriginalValues;
                    logger.LogWarning($"Original CreatedAt: {originalValues["CreatedAt"]}");
                    logger.LogWarning($"Current CreatedAt: {entry.CurrentValues["CreatedAt"]}");

                    // Fix: If it's a new entity being tracked as Modified
                    if (bankAccount.CreatedAt == default(DateTime) ||
                        (DateTime)originalValues["CreatedAt"] == default(DateTime))
                    {
                        logger.LogWarning($"FIXING: Changing state from Modified to Added");
                        entry.State = EntityState.Added;
                    }
                }
            }

            // Log all tracked entities
            var entries = context.ChangeTracker.Entries().ToList();
            foreach (var entry in entries)
            {
                logger.LogInformation($"Entity: {entry.Entity.GetType().Name}, State: {entry.State}");
            }



            var result = await context.SaveChangesAsync(cancellationToken);
            return result;
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
        // Collect events before commit
        var domainEvents = CollectDomainEvents();
        if (_currentTransaction == null)
            throw new InvalidOperationException("No transaction in progress");

        try
        {
            await _currentTransaction.CommitAsync(cancellationToken);
            // Publish events after successful commit
            if (domainEvents.Any())
            {
                await domainEventPublisher.PublishEventsAsync(domainEvents, cancellationToken);
            }
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
    #region Private Methods

    /// <summary>
    /// Collect domain events from all tracked entities
    /// CRITICAL: Must be called BEFORE SaveChanges to avoid losing events
    /// </summary>
    private List<BuildingBlocks.MessagingEvent.Base.IntegrationEvent> CollectDomainEvents()
    {
        var domainEvents = new List<BuildingBlocks.MessagingEvent.Base.IntegrationEvent>();

        // Find all entities that have domain events
        var entitiesWithEvents = context.ChangeTracker.Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Any())
            .ToList();

        foreach (var entry in entitiesWithEvents)
        {
            var events = entry.Entity.DomainEvents.ToList();
            domainEvents.AddRange(events);

            // Clear events after collecting (important to avoid double-publishing)
            entry.Entity.ClearDomainEvents();

            logger.LogDebug(
                "Collected {EventCount} domain events from {EntityType} [ID: {EntityId}]",
                events.Count, entry.Entity.GetType().Name /*, entry.Entity.Id*/);
        }

        return domainEvents;
    }

    #endregion

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        context.Dispose();
    }
}