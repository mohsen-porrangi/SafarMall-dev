using PaymentGateway.API.Models;
using System.Text.Json;

namespace PaymentGateway.API.Common;

/// <summary>
/// Extensions برای PaymentGateway
/// </summary>
public static class Extensions
{
    /// <summary>
    /// تولید PaymentId یکتا
    /// </summary>
    public static string GeneratePaymentId()
    {
        var now = DateTime.UtcNow;
        var dateStr = now.ToString("yyyyMMdd");
        var timeStr = now.ToString("HHmmss");
        var randomPart = Random.Shared.Next(1000, 9999);

        return $"PAY-{dateStr}-{timeStr}-{randomPart}";
    }

    /// <summary>
    /// دریافت IP آدرس کلاینت
    /// </summary>
    public static string GetClientIpAddress(this HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// تبدیل Headers به Dictionary
    /// </summary>
    public static Dictionary<string, string> ToDictionary(this IHeaderDictionary headers)
    {
        return headers.ToDictionary(
            h => h.Key,
            h => string.Join(", ", h.Value.ToArray()));
    }

    /// <summary>
    /// سریالایز JSON با تنظیمات مناسب
    /// </summary>
    public static string ToJson(this object obj)
    {
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }

    /// <summary>
    /// دیسریالایز JSON
    /// </summary>
    public static T? FromJson<T>(this string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// بررسی انقضا پرداخت
    /// </summary>
    public static bool IsExpired(this Payment payment)
    {
        return DateTime.UtcNow > payment.ExpiresAt;
    }

    /// <summary>
    /// محاسبه زمان باقی‌مانده
    /// </summary>
    public static TimeSpan GetRemainingTime(this Payment payment)
    {
        var remaining = payment.ExpiresAt - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    /// <summary>
    /// فرمت مبلغ ریالی
    /// </summary>
    public static string FormatRials(this decimal amount)
    {
        return $"{amount:N0} ریال";
    }

    /// <summary>
    /// تولید کد پیگیری تصادفی
    /// </summary>
    public static string GenerateTrackingCode()
    {
        return Random.Shared.Next(100000, 999999).ToString();
    }

    /// <summary>
    /// ماسک کردن شناسه حساس
    /// </summary>
    public static string MaskSensitiveData(this string data, int visibleChars = 4)
    {
        if (string.IsNullOrEmpty(data) || data.Length <= visibleChars)
            return data;

        var visiblePart = data.Substring(data.Length - visibleChars);
        var maskedPart = new string('*', Math.Max(0, data.Length - visibleChars));

        return maskedPart + visiblePart;
    }

    /// <summary>
    /// بررسی صحت GUID
    /// </summary>
    public static bool IsValidGuid(this string value)
    {
        return Guid.TryParse(value, out _);
    }
}