using BuildingBlocks.Contracts;
using BuildingBlocks.Domain;
using BuildingBlocks.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using PaymentGateway.API.Models;

/// <summary>
/// موجودیت پرداخت
/// </summary>
public class Payment : BaseEntity<Guid>, ISoftDelete
{

    /// <summary>
    /// شناسه یکتای پرداخت در سیستم
    /// </summary>
    public string PaymentId { get; set; } = string.Empty;

    /// <summary>
    /// نوع درگاه پرداخت
    /// </summary>
    public PaymentGatewayType GatewayType { get; set; }

    /// <summary>
    /// واحد پولی (Authority)
    /// </summary>
    public CurrencyCode Currency { get; set; }

    /// <summary>
    /// شناسه مرجع درگاه (Authority)
    /// </summary>
    public string GatewayReference { get; set; } = string.Empty;

    /// <summary>
    /// شناسه کاربر - اضافه شده برای linking به WalletApp
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// مبلغ پرداخت (ریال)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// توضیحات پرداخت
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// آدرس بازگشت
    /// </summary>
    public string? CallbackUrl { get; set; } = string.Empty;

    /// <summary>
    /// شناسه سفارش (اختیاری)
    /// </summary>
    public string? OrderId { get; set; }

    /// <summary>
    /// وضعیت پرداخت
    /// </summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>
    /// شناسه مرجع تراکنش (پس از تایید)
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// کد پیگیری درگاه
    /// </summary>
    public string? TrackingCode { get; set; }

    /// <summary>
    /// پیام خطا (در صورت ناموفق بودن)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// کد خطای درگاه
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// اعتبار سنجی
    /// </summary>
    public bool IsVerified { get; set; } = false;

    /// <summary>
    /// زمان پرداخت
    /// </summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>
    /// زمان انقضا
    /// </summary>
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(30);

    /// <summary>
    /// تعداد تلاش برای تایید
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// حداکثر تعداد تلاش
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// بررسی انقضا
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// بررسی امکان retry
    /// </summary>
    public bool CanRetry => RetryCount < MaxRetries && Status == PaymentStatus.Pending;

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// علامت‌گذاری به عنوان پرداخت شده
    /// </summary>
    public void MarkAsPaid(string transactionId, string? trackingCode = null)
    {
        Status = PaymentStatus.Paid;
        IsVerified = true;
        TransactionId = transactionId;
        TrackingCode = trackingCode;
        PaidAt = DateTime.UtcNow;
        ErrorMessage = null;
        ErrorCode = null;
    }

    /// <summary>
    /// علامت‌گذاری به عنوان کنسل شده
    /// </summary>
    public void MarkAsCancelled(string? reason = null)
    {
        Status = PaymentStatus.Cancelled;
        ErrorMessage = reason ?? "Payment cancelled by user";
        ErrorCode = "CANCELLED";
    }

    /// <summary>
    /// علامت‌گذاری به عنوان ناموفق
    /// </summary>
    public void MarkAsFailed(string? errorMessage = null, string? errorCode = null)
    {
        Status = PaymentStatus.Failed;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// افزایش تعداد تلاش
    /// </summary>
    public void IncrementRetry()
    {
        RetryCount++;
    }
}