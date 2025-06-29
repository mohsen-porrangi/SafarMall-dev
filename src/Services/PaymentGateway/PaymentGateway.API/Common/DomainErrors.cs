namespace PaymentGateway.API.Common;

/// <summary>
/// کدهای خطا و پیام‌های دامنه
/// </summary>
public static class DomainErrors
{
    public static class Payment
    {
        public const string InvalidAmount = "PAYMENT_INVALID_AMOUNT";
        public const string InvalidCallback = "PAYMENT_INVALID_CALLBACK";
        public const string GatewayNotSupported = "PAYMENT_GATEWAY_NOT_SUPPORTED";
        public const string PaymentNotFound = "PAYMENT_NOT_FOUND";
        public const string PaymentExpired = "PAYMENT_EXPIRED";
        public const string PaymentAlreadyProcessed = "PAYMENT_ALREADY_PROCESSED";
        public const string PaymentFailed = "PAYMENT_FAILED";
        public const string VerificationFailed = "PAYMENT_VERIFICATION_FAILED";

        public static readonly Dictionary<string, string> Messages = new()
        {
            [InvalidAmount] = "مبلغ پرداخت نامعتبر است",
            [InvalidCallback] = "آدرس بازگشت نامعتبر است",
            [GatewayNotSupported] = "درگاه پرداخت پشتیبانی نمی‌شود",
            [PaymentNotFound] = "پرداخت یافت نشد",
            [PaymentExpired] = "پرداخت منقضی شده است",
            [PaymentAlreadyProcessed] = "پرداخت قبلاً پردازش شده است",
            [PaymentFailed] = "پرداخت ناموفق بود",
            [VerificationFailed] = "تایید پرداخت ناموفق بود"
        };
    }

    public static class Webhook
    {
        public const string InvalidSignature = "WEBHOOK_INVALID_SIGNATURE";
        public const string InvalidPayload = "WEBHOOK_INVALID_PAYLOAD";
        public const string ProcessingFailed = "WEBHOOK_PROCESSING_FAILED";
        public const string UnauthorizedSource = "WEBHOOK_UNAUTHORIZED_SOURCE";

        public static readonly Dictionary<string, string> Messages = new()
        {
            [InvalidSignature] = "امضای Webhook نامعتبر است",
            [InvalidPayload] = "محتوای Webhook نامعتبر است",
            [ProcessingFailed] = "پردازش Webhook ناموفق بود",
            [UnauthorizedSource] = "منبع Webhook غیرمجاز است"
        };
    }

    public static class Gateway
    {
        public const string CommunicationError = "GATEWAY_COMMUNICATION_ERROR";
        public const string TimeoutError = "GATEWAY_TIMEOUT_ERROR";
        public const string ConfigurationError = "GATEWAY_CONFIGURATION_ERROR";
        public const string ServiceUnavailable = "GATEWAY_SERVICE_UNAVAILABLE";

        public static readonly Dictionary<string, string> Messages = new()
        {
            [CommunicationError] = "خطا در ارتباط با درگاه پرداخت",
            [TimeoutError] = "درگاه پرداخت پاسخ نداد",
            [ConfigurationError] = "تنظیمات درگاه پرداخت نامعتبر است",
            [ServiceUnavailable] = "درگاه پرداخت در دسترس نیست"
        };
    }

    /// <summary>
    /// دریافت پیام خطا بر اساس کد
    /// </summary>
    public static string GetMessage(string errorCode)
    {
        var allMessages = new Dictionary<string, string>();

        foreach (var category in new[] {
            Payment.Messages,
            Webhook.Messages,
            Gateway.Messages
        })
        {
            foreach (var kvp in category)
            {
                allMessages[kvp.Key] = kvp.Value;
            }
        }

        return allMessages.TryGetValue(errorCode, out var message)
            ? message
            : "خطای نامشخص";
    }
}