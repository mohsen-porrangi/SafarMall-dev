using BuildingBlocks.Domain;
using BuildingBlocks.Enums;

namespace PaymentGateway.API.Models;

/// <summary>
/// لاگ Webhook ها
/// </summary>
public class WebhookLog : BaseEntity<Guid>
{

    /// <summary>
    /// نوع درگاه پرداخت
    /// </summary>
    public PaymentGatewayType GatewayType { get; set; }

    /// <summary>
    /// نوع رویداد
    /// </summary>
    public WebhookEventType EventType { get; set; }

    /// <summary>
    /// شناسه پرداخت
    /// </summary>
    public string? PaymentId { get; set; }

    /// <summary>
    /// محتوای درخواست
    /// </summary>
    public string RequestBody { get; set; } = string.Empty;

    /// <summary>
    /// هدرهای HTTP
    /// </summary>
    public string RequestHeaders { get; set; } = string.Empty;

    /// <summary>
    /// IP فرستنده
    /// </summary>
    public string SourceIp { get; set; } = string.Empty;

    /// <summary>
    /// وضعیت پردازش
    /// </summary>
    public bool IsProcessed { get; set; } = false;

    /// <summary>
    /// کد وضعیت پاسخ
    /// </summary>
    public int ResponseStatusCode { get; set; }

    /// <summary>
    /// پیام خطا (در صورت وجود)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// زمان دریافت
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// زمان پردازش
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    public WebhookLog()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// علامت‌گذاری به عنوان پردازش شده
    /// </summary>
    public void MarkAsProcessed(int statusCode = 200)
    {
        IsProcessed = true;
        ResponseStatusCode = statusCode;
        ProcessedAt = DateTime.UtcNow;
        ErrorMessage = null;
    }

    /// <summary>
    /// علامت‌گذاری به عنوان خطا
    /// </summary>
    public void MarkAsError(string errorMessage, int statusCode = 400)
    {
        IsProcessed = false;
        ErrorMessage = errorMessage;
        ResponseStatusCode = statusCode;
        ProcessedAt = DateTime.UtcNow;
    }
}