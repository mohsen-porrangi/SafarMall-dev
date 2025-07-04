using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentGateway.API.Models;

namespace PaymentGateway.API.Data.Configurations;

/// <summary>
/// تنظیمات Entity Framework برای WebhookLog
/// </summary>
public class WebhookLogConfiguration : IEntityTypeConfiguration<WebhookLog>
{
    public void Configure(EntityTypeBuilder<WebhookLog> builder)
    {
        // Table configuration
        builder.ToTable("WebhookLogs");
        builder.HasKey(w => w.Id);

        // Properties
        builder.Property(w => w.Id)
            .IsRequired();

        builder.Property(w => w.GatewayType)
            .HasConversion<int>()
            .IsRequired()
            .HasComment("نوع درگاه پرداخت");

        builder.Property(w => w.EventType)
            .HasConversion<int>()
            .IsRequired()
            .HasComment("نوع رویداد");

        builder.Property(w => w.PaymentId)
            .HasMaxLength(50)
            .IsRequired(false)
            .HasComment("شناسه پرداخت");

        builder.Property(w => w.RequestBody)
            .HasColumnType("nvarchar(max)")
            .IsRequired()
            .HasComment("محتوای درخواست");

        builder.Property(w => w.RequestHeaders)
            .HasColumnType("nvarchar(max)")
            .IsRequired()
            .HasComment("هدرهای HTTP");

        builder.Property(w => w.SourceIp)
            .HasMaxLength(45) // IPv6 support
            .IsRequired()
            .HasComment("IP فرستنده");

        builder.Property(w => w.IsProcessed)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("آیا پردازش شده؟");

        builder.Property(w => w.ResponseStatusCode)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("کد وضعیت پاسخ");

        builder.Property(w => w.ErrorMessage)
            .HasMaxLength(1000)
            .IsRequired(false)
            .HasComment("پیام خطا");

        builder.Property(w => w.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2(7)")
            .HasComment("زمان دریافت");

        builder.Property(w => w.ProcessedAt)
            .HasColumnType("datetime2(7)")
            .IsRequired(false)
            .HasComment("زمان پردازش");

        // Indexes
        builder.HasIndex(w => w.GatewayType)
            .HasDatabaseName("IX_WebhookLogs_GatewayType");

        builder.HasIndex(w => w.PaymentId)
            .HasDatabaseName("IX_WebhookLogs_PaymentId");

        builder.HasIndex(w => w.CreatedAt)
            .HasDatabaseName("IX_WebhookLogs_CreatedAt");

        builder.HasIndex(w => w.IsProcessed)
            .HasDatabaseName("IX_WebhookLogs_IsProcessed");

        builder.HasIndex(w => new { w.GatewayType, w.CreatedAt })
            .HasDatabaseName("IX_WebhookLogs_GatewayType_CreatedAt");
    }
}