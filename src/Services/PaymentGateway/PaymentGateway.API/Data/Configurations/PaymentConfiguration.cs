using BuildingBlocks.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentGateway.API.Models;

namespace PaymentGateway.API.Data.Configurations;

/// <summary>
/// تنظیمات Entity Framework برای Payment
/// </summary>
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        // Table configuration
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.Id)
            .IsRequired();

        builder.Property(p => p.PaymentId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.GatewayType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(p => p.GatewayReference)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(p => p.CallbackUrl)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(p => p.OrderId)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(p => p.Status)
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(PaymentStatus.Pending);

        builder.Property(p => p.TransactionId)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(p => p.TrackingCode)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(p => p.ErrorMessage)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(p => p.ErrorCode)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2(7)");

        builder.Property(p => p.UpdatedAt)
            .IsRequired()
            .HasColumnType("datetime2(7)");

        builder.Property(p => p.PaidAt)
            .HasColumnType("datetime2(7)")
            .IsRequired(false);

        builder.Property(p => p.ExpiresAt)
            .IsRequired()
            .HasColumnType("datetime2(7)");

        builder.Property(p => p.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.MaxRetries)
            .IsRequired()
            .HasDefaultValue(3);

        builder.Property(p => p.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(p => p.PaymentId)
            .IsUnique()
            .HasDatabaseName("IX_Payments_PaymentId");

        builder.HasIndex(p => p.GatewayReference)
            .HasDatabaseName("IX_Payments_GatewayReference");

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("IX_Payments_Status");

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("IX_Payments_CreatedAt");

        builder.HasIndex(p => p.ExpiresAt)
            .HasDatabaseName("IX_Payments_ExpiresAt");

        builder.HasIndex(p => new { p.Status, p.ExpiresAt })
            .HasDatabaseName("IX_Payments_Status_ExpiresAt");

        builder.HasIndex(p => p.IsDeleted)
            .HasDatabaseName("IX_Payments_IsDeleted");
        builder.Property(p => p.IsVerified)
            .IsRequired()
            .HasDefaultValue(false);
    }
}