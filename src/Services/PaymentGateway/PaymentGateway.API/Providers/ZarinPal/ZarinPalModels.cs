using System.Text.Json.Serialization;

namespace PaymentGateway.API.Providers.ZarinPal;

/// <summary>
/// درخواست ایجاد پرداخت زرین‌پال
/// </summary>
public record ZarinPalCreateRequest
{
    [JsonPropertyName("merchant_id")]
    public string MerchantId { get; init; } = string.Empty;

    [JsonPropertyName("amount")]
    public long Amount { get; init; }

    [JsonPropertyName("callback_url")]
    public string CallbackUrl { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("metadata")]
    public ZarinPalMetadata? Metadata { get; init; }
}

/// <summary>
/// متادیتای زرین‌پال
/// </summary>
public record ZarinPalMetadata
{
    [JsonPropertyName("order_id")]
    public string? OrderId { get; init; }
}

/// <summary>
/// پاسخ ایجاد پرداخت زرین‌پال
/// </summary>
public record ZarinPalCreateResponse
{
    [JsonPropertyName("data")]
    public ZarinPalCreateData? Data { get; init; }

    [JsonPropertyName("errors")]
    public ZarinPalError[]? Errors { get; init; }
}

/// <summary>
/// داده‌های ایجاد پرداخت زرین‌پال
/// </summary>
public record ZarinPalCreateData
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("authority")]
    public string Authority { get; init; } = string.Empty;

    [JsonPropertyName("fee_type")]
    public string FeeType { get; init; } = string.Empty;

    [JsonPropertyName("fee")]
    public long Fee { get; init; }
}

/// <summary>
/// درخواست تایید پرداخت زرین‌پال
/// </summary>
public record ZarinPalVerifyRequest
{
    [JsonPropertyName("merchant_id")]
    public string MerchantId { get; init; } = string.Empty;

    [JsonPropertyName("amount")]
    public long Amount { get; init; }

    [JsonPropertyName("authority")]
    public string Authority { get; init; } = string.Empty;
}

/// <summary>
/// پاسخ تایید پرداخت زرین‌پال
/// </summary>
public record ZarinPalVerifyResponse
{
    [JsonPropertyName("data")]
    public ZarinPalVerifyData? Data { get; init; }

    [JsonPropertyName("errors")]
    public ZarinPalError[]? Errors { get; init; }
}

/// <summary>
/// داده‌های تایید پرداخت زرین‌پال
/// </summary>
public record ZarinPalVerifyData
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("card_hash")]
    public string CardHash { get; init; } = string.Empty;

    [JsonPropertyName("card_pan")]
    public string CardPan { get; init; } = string.Empty;

    [JsonPropertyName("ref_id")]
    public long RefId { get; init; }

    [JsonPropertyName("fee_type")]
    public string FeeType { get; init; } = string.Empty;

    [JsonPropertyName("fee")]
    public long Fee { get; init; }
}

/// <summary>
/// خطای زرین‌پال
/// </summary>
public record ZarinPalError
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("validations")]
    public ZarinPalValidation[]? Validations { get; init; }
}

/// <summary>
/// اعتبارسنجی زرین‌پال
/// </summary>
public record ZarinPalValidation
{
    [JsonPropertyName("field")]
    public string Field { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// کدهای وضعیت زرین‌پال
/// </summary>
public static class ZarinPalStatusCodes
{
    public const int Success = 100;
    public const int AlreadyVerified = 101;
    public const int InvalidMerchant = -9;
    public const int InvalidIP = -10;
    public const int InvalidAmount = -11;
    public const int InvalidAuthority = -12;
    public const int TransactionNotFound = -50;
    public const int DuplicateTransaction = -51;

    public static string GetMessage(int code) => code switch
    {
        Success => "تراکنش با موفقیت انجام شد",
        AlreadyVerified => "تراکنش قبلاً تایید شده",
        InvalidMerchant => "پذیرنده نامعتبر",
        InvalidIP => "IP نامعتبر",
        InvalidAmount => "مبلغ نامعتبر",
        InvalidAuthority => "Authority نامعتبر",
        TransactionNotFound => "تراکنش یافت نشد",
        DuplicateTransaction => "تراکنش تکراری",
        _ => "خطای نامشخص"
    };
}