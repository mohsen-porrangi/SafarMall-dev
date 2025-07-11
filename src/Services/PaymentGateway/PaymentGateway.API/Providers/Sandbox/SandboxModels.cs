using BuildingBlocks.Enums;
using PaymentGateway.API.Models;

namespace PaymentGateway.API.Providers.Sandbox;

/// <summary>
/// مدل‌های مربوط به درگاه تستی Sandbox
/// </summary>
public static class SandboxModels
{
    /// <summary>
    /// درخواست ایجاد پرداخت Sandbox
    /// </summary>
    public record SandboxCreateRequest
    {
        public decimal Amount { get; init; }
        public string Description { get; init; } = string.Empty;
        public string CallbackUrl { get; init; } = string.Empty;
        public string? OrderId { get; init; }
    }

    /// <summary>
    /// پاسخ ایجاد پرداخت Sandbox
    /// </summary>
    public record SandboxCreateResponse
    {
        public bool Success { get; init; }
        public string? Authority { get; init; }
        public string? PaymentUrl { get; init; }
        public string? ErrorMessage { get; init; }
        public int ErrorCode { get; init; }
    }

    /// <summary>
    /// درخواست تایید پرداخت Sandbox
    /// </summary>
    public record SandboxVerifyRequest
    {
        public string Authority { get; init; } = string.Empty;
        public decimal Amount { get; init; }
    }

    /// <summary>
    /// پاسخ تایید پرداخت Sandbox
    /// </summary>
    public record SandboxVerifyResponse
    {
        public bool Success { get; init; }
        public string? RefId { get; init; }
        public decimal Amount { get; init; }
        public string? ErrorMessage { get; init; }
        public int ErrorCode { get; init; }
    }

    /// <summary>
    /// داده‌های پرداخت Sandbox که در حافظه نگهداری می‌شود
    /// </summary>
    public class SandboxPaymentData
    {
        public string Authority { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;        
        public string? OrderId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(30);
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string? RefId { get; set; }
        public string? TrackingCode { get; set; }
        public DateTime? PaidAt { get; set; }

        /// <summary>
        /// بررسی انقضا
        /// </summary>
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;

        /// <summary>
        /// تولید شناسه مرجع جدید
        /// </summary>
        public static string GenerateAuthority()
        {
            return Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// تولید شناسه تراکنش
        /// </summary>
        public static string GenerateRefId()
        {
            return Random.Shared.Next(100000, 999999).ToString();
        }
    }

    /// <summary>
    /// کدهای خطای Sandbox (شبیه‌سازی درگاه‌های واقعی)
    /// </summary>
    public static class ErrorCodes
    {
        public const int Success = 100;
        public const int InvalidAmount = -1;
        public const int InvalidMerchant = -2;
        public const int PaymentNotFound = -3;
        public const int PaymentExpired = -4;
        public const int PaymentAlreadyVerified = -5;
        public const int GeneralError = -99;

        public static readonly Dictionary<int, string> Messages = new()
        {
            [Success] = "عملیات با موفقیت انجام شد",
            [InvalidAmount] = "مبلغ نامعتبر است",
            [InvalidMerchant] = "کد فروشنده نامعتبر است",
            [PaymentNotFound] = "پرداخت یافت نشد",
            [PaymentExpired] = "پرداخت منقضی شده است",
            [PaymentAlreadyVerified] = "پرداخت قبلاً تایید شده است",
            [GeneralError] = "خطای عمومی"
        };

        public static string GetMessage(int errorCode)
        {
            return Messages.TryGetValue(errorCode, out var message)
                ? message
                : "خطای نامشخص";
        }
    }
}