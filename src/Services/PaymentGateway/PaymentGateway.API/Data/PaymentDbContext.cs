using BuildingBlocks.Extensions;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.API.Models;

namespace PaymentGateway.API.Data;

/// <summary>
/// کانتکست دیتابیس درگاه پرداخت
/// </summary>
public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<WebhookLog> WebhookLogs => Set<WebhookLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);

        // Set default schema 
        modelBuilder.HasDefaultSchema("payment");
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Apply audit properties from BuildingBlocks
            ChangeTracker.SetAuditProperties();

            return await base.SaveChangesAsync(cancellationToken);
        }
        catch(Exception ex)
        {
            throw;
        }
        
    }

    public override int SaveChanges()
    {
        // Apply audit properties from BuildingBlocks
        ChangeTracker.SetAuditProperties();

        return base.SaveChanges();
    }
}