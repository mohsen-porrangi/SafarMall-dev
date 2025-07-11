using BuildingBlocks.Enums;
using PaymentGateway.API.Models;

namespace PaymentGateway.API.Common;

/// <summary>
/// قوانین کسب و کار درگاه پرداخت
/// </summary>
public static class BusinessRules
{
    public static class Payment
    {
        /// <summary>
        /// حداقل مبلغ پرداخت (ریال)
        /// </summary>
        public const decimal MinimumAmount = 1000;

        /// <summary>
        /// حداکثر مبلغ پرداخت (ریال)
        /// </summary>
        public const decimal MaximumAmount = 500_000_000; //TODO Max amount

        /// <summary>
        /// زمان انقضا پرداخت (دقیقه)
        /// </summary>
        public const int ExpirationMinutes = 30;

        /// <summary>
        /// حداکثر تعداد تلاش مجدد
        /// </summary>
        public const int MaxRetries = 3;

        /// <summary>
        /// بررسی اعتبار مبلغ
        /// </summary>
        public static bool IsValidAmount(decimal amount)
        {
            return amount >= MinimumAmount &&
                   amount <= MaximumAmount &&
                   amount % 1 == 0; // فقط عدد صحیح
        }

        /// <summary>
        /// بررسی اعتبار URL
        /// </summary>
        public static bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }

    public static class Webhook
    {
        /// <summary>
        /// مدت نگهداری لاگ‌های Webhook (روز)
        /// </summary>
        public const int LogRetentionDays = 30;

        /// <summary>
        /// حداکثر اندازه محتوای Webhook (بایت)
        /// </summary>
        public const int MaxContentSize = 1024 * 1024; // 1MB

        /// <summary>
        /// IP های مجاز برای ZarinPal
        /// </summary>
        public static readonly string[] ZarinPalIPs =
        {
            "31.7.63.68",
            "31.7.63.69",
            "46.209.4.68",
            "46.209.4.69"
        };

        /// <summary>
        /// IP های مجاز برای Zibal
        /// </summary>
        public static readonly string[] ZibalIPs =
        {
            "185.8.172.35",
            "185.8.174.174"
        };

        /// <summary>
        /// بررسی IP مجاز
        /// </summary>
        public static bool IsAllowedIP(string ip, PaymentGatewayType gateway)
        {
            return gateway switch
            {
                PaymentGatewayType.ZarinPal => ZarinPalIPs.Contains(ip),
                PaymentGatewayType.Zibal => ZibalIPs.Contains(ip),
                PaymentGatewayType.Sandbox => true, // همه IP ها مجاز
                _ => false
            };
        }
    }

    public static class RateLimit
    {
        /// <summary>
        /// تعداد درخواست مجاز در دقیقه
        /// </summary>
        public const int RequestsPerMinute = 60;

        /// <summary>
        /// تعداد درخواست Webhook مجاز در دقیقه
        /// </summary>
        public const int WebhookRequestsPerMinute = 120;
    }

    public static class Cache
    {
        /// <summary>
        /// مدت Cache برای وضعیت پرداخت (دقیقه)
        /// </summary>
        public const int PaymentStatusCacheMinutes = 15;

        /// <summary>
        /// مدت Cache برای تنظیمات درگاه (ساعت)
        /// </summary>
        public const int GatewayConfigCacheHours = 24;
    }
}