using System.Text.Json.Serialization;

namespace PaymentGateway.API.Providers.Zibal;

/// <summary>
/// درخواست ایجاد پرداخت زیبال
/// </summary>
public record ZibalCreateRequest
{
    [JsonPropertyName("merchant")]
    public string Merchant { get; init; } = string.Empty;

    [JsonPropertyName("amount")]
    public long Amount { get; init; }

    [JsonPropertyName("callbackUrl")]
    public string CallbackUrl { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("orderId")]
    public string? OrderId { get; init; }
}

/// <summary>
/// پاسخ ایجاد پرداخت زیبال
/// </summary>
public record ZibalCreateResponse
{
    [JsonPropertyName("result")]
    public int Result { get; init; }

    [JsonPropertyName("trackId")]
    public long TrackId { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// درخواست تایید پرداخت زیبال
/// </summary>
public record ZibalVerifyRequest
{
    [JsonPropertyName("merchant")]
    public string Merchant { get; init; } = string.Empty;

    [JsonPropertyName("trackId")]
    public long TrackId { get; init; }
}

/// <summary>
/// پاسخ تایید پرداخت زیبال
/// </summary>
public record ZibalVerifyResponse
{
    [JsonPropertyName("result")]
    public int Result { get; init; }

    [JsonPropertyName("amount")]
    public long Amount { get; init; }

    [JsonPropertyName("refNumber")]
    public string? RefNumber { get; init; } 

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("cardNumber")]
    public string CardNumber { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public int Status { get; init; }

    [JsonPropertyName("orderId")]
    public string? OrderId { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// کدهای خطای زیبال
/// </summary>
public static class ZibalResultCodes
{
    public const int Success = 100;
    public const int AlreadyVerified = 201;
    public const int MerchantNotFound = -1;
    public const int MerchantInactive = -2;
    public const int InvalidAmount = -3;
    public const int InvalidMerchant = -4;
    public const int InvalidCallback = -5;
    public const int TransactionNotFound = -102;
    public const int AlreadyPaid = -103;

    public static string GetMessage(int code) => code switch
    {
        Success => "موفق",
        AlreadyVerified => "قبلاً تایید شده",
        MerchantNotFound => "پذیرنده یافت نشد",
        MerchantInactive => "پذیرنده غیرفعال",
        InvalidAmount => "مبلغ نامعتبر",
        InvalidMerchant => "پذیرنده نامعتبر",
        InvalidCallback => "آدرس بازگشت نامعتبر",
        TransactionNotFound => "تراکنش یافت نشد",
        AlreadyPaid => "قبلاً پرداخت شده",
        _ => "خطای نامشخص"
    };
}