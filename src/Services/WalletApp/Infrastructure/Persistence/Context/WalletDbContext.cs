using BuildingBlocks.Extensions;
using Microsoft.EntityFrameworkCore;
using WalletApp.Application.Common.Interfaces;
using WalletApp.Domain.Aggregates.TransactionAggregate;
using WalletApp.Domain.Aggregates.WalletAggregate;

namespace WalletApp.Infrastructure.Persistence.Context;

/// <summary>
/// Database context for Wallet domain
/// </summary>
public class WalletDbContext : DbContext, IWalletDbContext
{
    public WalletDbContext(DbContextOptions<WalletDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<CurrencyAccount> CurrencyAccounts => Set<CurrencyAccount>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<Credit> Credits => Set<Credit>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionSnapshot> TransactionSnapshots => Set<TransactionSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WalletDbContext).Assembly);

        // Set default schema
        modelBuilder.HasDefaultSchema("wallet");
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Apply audit properties
        ChangeTracker.SetAuditProperties();

        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        // Apply audit properties
        ChangeTracker.SetAuditProperties();

        return base.SaveChanges();
    }
}