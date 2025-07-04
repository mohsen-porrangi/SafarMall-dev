using BuildingBlocks.Enums;
using BuildingBlocks.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using WalletApp.Application.Common.Interfaces;

namespace WalletApp.Infrastructure.Services;

/// <summary>
/// Payment gateway client implementation
/// </summary>
public class PaymentGatewayClient(
    HttpClient httpClient,
    ILogger<PaymentGatewayClient> logger,
    IOptions<PaymentGatewaySettings> settings) : IPaymentGatewayClient
{
    private readonly PaymentGatewaySettings _settings = settings.Value;

    public async Task<PaymentResult> CreatePaymentAsync(
        Money amount,
        string description,
        string callbackUrl,
        PaymentGatewayType gateway = PaymentGatewayType.Zibal,
        string? orderId = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating payment: Amount={Amount}, Gateway={Gateway}, OrderId={OrderId}",
            amount, gateway, orderId);

        try
        {
            return gateway switch
            {
                PaymentGatewayType.Sandbox => await CreateSandboxPaymentAsync(amount, description, callbackUrl, orderId, cancellationToken),
                PaymentGatewayType.Zibal => await CreateZibalPaymentAsync(amount, description, callbackUrl, orderId, cancellationToken),
                PaymentGatewayType.ZarinPal => await CreateMockZarinPalPaymentAsync(amount, description, callbackUrl, orderId, cancellationToken),
                _ => throw new ArgumentException($"Gateway {gateway} is not supported", nameof(gateway))
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create payment for amount {Amount}", amount);
            return new PaymentResult
            {
                IsSuccessful = false,
                ErrorMessage = "خطا در ایجاد درخواست پرداخت",
                ErrorCode = "GATEWAY_ERROR"
            };
        }
    }

    public async Task<PaymentVerificationResult> VerifyPaymentAsync(
        string authority,
        Money expectedAmount,
        PaymentGatewayType gateway = PaymentGatewayType.Zibal,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Verifying payment: Authority={Authority}, Amount={Amount}, Gateway={Gateway}",
            authority, expectedAmount, gateway);

        try
        {
            return gateway switch
            {
                PaymentGatewayType.Sandbox => await VerifySandboxPaymentAsync(authority, expectedAmount, cancellationToken),
                PaymentGatewayType.Zibal => await VerifyZibalPaymentAsync(authority, expectedAmount, cancellationToken),
                PaymentGatewayType.ZarinPal => await VerifyMockZarinPalPaymentAsync(authority, expectedAmount, cancellationToken),
                _ => throw new ArgumentException($"Gateway {gateway} is not supported", nameof(gateway))
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to verify payment for authority {Authority}", authority);
            return new PaymentVerificationResult
            {
                IsSuccessful = false,
                IsVerified = false,
                ErrorMessage = "خطا در تایید پرداخت",
                ErrorCode = "VERIFICATION_ERROR"
            };
        }
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(
        string authority,
        PaymentGatewayType gateway = PaymentGatewayType.Zibal,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting payment status: Authority={Authority}, Gateway={Gateway}", authority, gateway);

        try
        {
            return gateway switch
            {
                PaymentGatewayType.Zibal => await GetZibalPaymentStatusAsync(authority, cancellationToken),
                _ => await GetMockPaymentStatusAsync(authority, cancellationToken)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get payment status for authority {Authority}", authority);
            return new PaymentStatusResult
            {
                IsSuccessful = false,
                Status = PaymentStatus.Failed,
                ErrorMessage = "خطا در دریافت وضعیت پرداخت"
            };
        }
    }

    #region Zibal Implementation

    private async Task<PaymentResult> CreateZibalPaymentAsync(
        Money amount,
        string description,
        string callbackUrl,
        string? orderId,
        CancellationToken cancellationToken)
    {
        var zibalConfig = _settings.Zibal;

        if (!zibalConfig.IsEnabled)
        {
            logger.LogWarning("Zibal gateway is disabled");
            return new PaymentResult
            {
                IsSuccessful = false,
                ErrorMessage = "درگاه زیبال غیرفعال است",
                ErrorCode = "GATEWAY_DISABLED"
            };
        }

        var requestData = new
        {
            merchant = zibalConfig.MerchantId,
            amount = (long)amount.Value, // Zibal expects amount in Rials
            description,
            callbackUrl,
            orderId = orderId ?? Guid.NewGuid().ToString()
        };

        var jsonContent = JsonSerializer.Serialize(requestData);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(zibalConfig.Timeout));
        using var combinedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        var response = await httpClient.PostAsync($"{zibalConfig.BaseUrl}/request", content, combinedCancellation.Token);
        var responseContent = await response.Content.ReadAsStringAsync(combinedCancellation.Token);

        logger.LogDebug("Zibal create payment response: {Response}", responseContent);

        var zibalResponse = JsonSerializer.Deserialize<ZibalCreatePaymentResponse>(responseContent);

        if (zibalResponse?.result == 100) // Success code for Zibal
        {
            var paymentUrl = $"{zibalConfig.CallbackBaseUrl}/{zibalResponse.trackId}";

            return new PaymentResult
            {
                IsSuccessful = true,
                Authority = zibalResponse.trackId.ToString(),
                PaymentUrl = paymentUrl
            };
        }

        var errorMessage = GetZibalErrorMessage(zibalResponse?.result ?? -1);
        logger.LogError("Zibal payment creation failed: Code={Code}, Message={Message}", zibalResponse?.result, errorMessage);

        return new PaymentResult
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage,
            ErrorCode = $"ZIBAL_ERROR_{zibalResponse?.result}"
        };
    }

    private async Task<PaymentVerificationResult> VerifyZibalPaymentAsync(
        string authority,
        Money expectedAmount,
        CancellationToken cancellationToken)
    {
        var zibalConfig = _settings.Zibal;

        if (!long.TryParse(authority, out var trackId))
        {
            logger.LogError("Invalid authority format for Zibal: {Authority}", authority);
            return new PaymentVerificationResult
            {
                IsSuccessful = false,
                IsVerified = false,
                ErrorMessage = "شناسه تراکنش نامعتبر است",
                ErrorCode = "INVALID_AUTHORITY"
            };
        }

        var requestData = new
        {
            merchant = zibalConfig.MerchantId,
            trackId
        };

        var jsonContent = JsonSerializer.Serialize(requestData);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(zibalConfig.Timeout));
        using var combinedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        var response = await httpClient.PostAsync($"{zibalConfig.BaseUrl}/verify", content, combinedCancellation.Token);
        var responseContent = await response.Content.ReadAsStringAsync(combinedCancellation.Token);

        logger.LogDebug("Zibal verify payment response: {Response}", responseContent);

        var zibalResponse = JsonSerializer.Deserialize<ZibalVerifyPaymentResponse>(responseContent);

        if (zibalResponse?.result == 100) // Success code for Zibal
        {
            var actualAmount = Money.FromIrr(zibalResponse.amount);

            // Check if amounts match
            if (Math.Abs(actualAmount.Value - expectedAmount.Value) > 0.01m)
            {
                logger.LogWarning("Amount mismatch in Zibal verification: Expected={Expected}, Actual={Actual}",
                    expectedAmount, actualAmount);

                return new PaymentVerificationResult
                {
                    IsSuccessful = false,
                    IsVerified = false,
                    ErrorMessage = "مبلغ پرداخت شده با مبلغ درخواستی مطابقت ندارد",
                    ErrorCode = "AMOUNT_MISMATCH",
                    ActualAmount = actualAmount
                };
            }

            return new PaymentVerificationResult
            {
                IsSuccessful = true,
                IsVerified = true,
                ReferenceId = zibalResponse.refNumber,
                ActualAmount = actualAmount,
                VerificationDate = DateTime.UtcNow
            };
        }

        var errorMessage = GetZibalErrorMessage(zibalResponse?.result ?? -1);
        logger.LogError("Zibal payment verification failed: Code={Code}, Message={Message}", zibalResponse?.result, errorMessage);

        return new PaymentVerificationResult
        {
            IsSuccessful = false,
            IsVerified = false,
            ErrorMessage = errorMessage,
            ErrorCode = $"ZIBAL_VERIFY_ERROR_{zibalResponse?.result}"
        };
    }

    private async Task<PaymentStatusResult> GetZibalPaymentStatusAsync(
        string authority,
        CancellationToken cancellationToken)
    {
        var zibalConfig = _settings.Zibal;

        if (!long.TryParse(authority, out var trackId))
        {
            logger.LogError("Invalid authority format for Zibal: {Authority}", authority);
            return new PaymentStatusResult
            {
                IsSuccessful = false,
                Status = PaymentStatus.Failed,
                ErrorMessage = "شناسه تراکنش نامعتبر است"
            };
        }

        var requestData = new
        {
            merchant = zibalConfig.MerchantId,
            trackId
        };

        var jsonContent = JsonSerializer.Serialize(requestData);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(zibalConfig.Timeout));
        using var combinedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        var response = await httpClient.PostAsync($"{zibalConfig.BaseUrl}/inquiry", content, combinedCancellation.Token);
        var responseContent = await response.Content.ReadAsStringAsync(combinedCancellation.Token);

        logger.LogDebug("Zibal payment status response: {Response}", responseContent);

        var zibalResponse = JsonSerializer.Deserialize<ZibalInquiryResponse>(responseContent);

        if (zibalResponse?.result == 100) // Success code for Zibal
        {
            var status = MapZibalStatusToPaymentStatus(zibalResponse.status);

            return new PaymentStatusResult
            {
                IsSuccessful = true,
                Status = status,
                Amount = Money.FromIrr(zibalResponse.amount),
                CreatedAt = DateTime.UtcNow, // Zibal doesn't provide creation date in inquiry
                PaidAt = status == PaymentStatus.Paid ? DateTime.UtcNow : null
            };
        }

        var errorMessage = GetZibalErrorMessage(zibalResponse?.result ?? -1);

        return new PaymentStatusResult
        {
            IsSuccessful = false,
            Status = PaymentStatus.Failed,
            ErrorMessage = errorMessage
        };
    }

    private static PaymentStatus MapZibalStatusToPaymentStatus(int zibalStatus)
    {
        return zibalStatus switch
        {
            1 => PaymentStatus.Paid,      // پرداخت شده
            2 => PaymentStatus.Failed,    // ناموفق
            3 => PaymentStatus.Pending,   // در انتظار
            4 => PaymentStatus.Cancelled, // لغو شده توسط کاربر
            5 => PaymentStatus.Failed,    // ناموفق
            6 => PaymentStatus.Cancelled, // برگشت داده شده
            7 => PaymentStatus.Processing, // در حال پردازش
            8 => PaymentStatus.Processing, // آماده پردازش
            _ => PaymentStatus.Failed
        };
    }

    private static string GetZibalErrorMessage(int resultCode)
    {
        return resultCode switch
        {
            100 => "عملیات موفق",
            102 => "merchant یافت نشد",
            103 => "merchant غیرفعال",
            104 => "merchant نامعتبر",
            105 => "amount بایستی بزرگتر از 1000 ریال باشد",
            106 => "callbackUrl نامعتبر میباشد",
            113 => "amount مبلغ تراکنش از سقف آن بیشتر است",
            201 => "قبلا تایید شده",
            202 => "سفارش پرداخت نشده یا ناموفق بوده است",
            203 => "trackId نامعتبر میباشد",
            _ => "خطای نامشخص در درگاه پرداخت"
        };
    }

    #endregion

    #region Zibal Response Models

    private class ZibalCreatePaymentResponse
    {
        public int result { get; set; }
        public long trackId { get; set; }
        public string? message { get; set; }
    }

    private class ZibalVerifyPaymentResponse
    {
        public int result { get; set; }
        public decimal amount { get; set; }
        public string? refNumber { get; set; }
        public string? description { get; set; }
        public string? cardNumber { get; set; }
        public int status { get; set; }
        public string? orderId { get; set; }
        public string? message { get; set; }
    }

    private class ZibalInquiryResponse
    {
        public int result { get; set; }
        public int status { get; set; }
        public decimal amount { get; set; }
        public string? refNumber { get; set; }
        public string? description { get; set; }
        public string? cardNumber { get; set; }
        public string? orderId { get; set; }
        public string? message { get; set; }
    }

    #endregion

    #region Mock Implementation Methods (Existing)

    private async Task<PaymentResult> CreateSandboxPaymentAsync(
        Money amount,
        string description,
        string callbackUrl,
        string? orderId,
        CancellationToken cancellationToken)
    {
        await Task.Delay(200, cancellationToken); // Simulate API call

        var authority = Guid.NewGuid().ToString();
        var paymentUrl = $"https://sandbox.zarinpal.com/pg/StartPay/{authority}";

        return new PaymentResult
        {
            IsSuccessful = true,
            Authority = authority,
            PaymentUrl = paymentUrl
        };
    }

    private async Task<PaymentResult> CreateMockZarinPalPaymentAsync(
        Money amount,
        string description,
        string callbackUrl,
        string? orderId,
        CancellationToken cancellationToken)
    {
        await Task.Delay(300, cancellationToken); // Simulate API call

        // Mock successful response
        var authority = Guid.NewGuid().ToString();
        var paymentUrl = $"https://www.zarinpal.com/pg/StartPay/{authority}";

        return new PaymentResult
        {
            IsSuccessful = true,
            Authority = authority,
            PaymentUrl = paymentUrl
        };
    }

    private async Task<PaymentVerificationResult> VerifySandboxPaymentAsync(
        string authority,
        Money expectedAmount,
        CancellationToken cancellationToken)
    {
        await Task.Delay(200, cancellationToken); // Simulate API call

        return new PaymentVerificationResult
        {
            IsSuccessful = true,
            IsVerified = true,
            ReferenceId = Random.Shared.Next(100000, 999999).ToString(),
            ActualAmount = expectedAmount,
            VerificationDate = DateTime.UtcNow
        };
    }

    private async Task<PaymentVerificationResult> VerifyMockZarinPalPaymentAsync(
        string authority,
        Money expectedAmount,
        CancellationToken cancellationToken)
    {
        await Task.Delay(300, cancellationToken); // Simulate API call

        return new PaymentVerificationResult
        {
            IsSuccessful = true,
            IsVerified = true,
            ReferenceId = Random.Shared.Next(100000, 999999).ToString(),
            ActualAmount = expectedAmount,
            VerificationDate = DateTime.UtcNow
        };
    }

    private async Task<PaymentStatusResult> GetMockPaymentStatusAsync(
        string authority,
        CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken); // Simulate API call

        return new PaymentStatusResult
        {
            IsSuccessful = true,
            Status = PaymentStatus.Paid,
            Amount = Money.Create(100000, CurrencyCode.IRR),
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            PaidAt = DateTime.UtcNow.AddMinutes(-2)
        };
    }

    #endregion
}

// Configuration classes (add these to your configuration setup)
public class PaymentGatewaySettings
{
    public ZibalSettings Zibal { get; set; } = new();
    public ZarinPalSettings ZarinPal { get; set; } = new();
    public SandboxSettings Sandbox { get; set; } = new();
}

public class ZibalSettings
{
    public string MerchantId { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string CallbackBaseUrl { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int Timeout { get; set; } = 30;
}

public class ZarinPalSettings
{
    public string MerchantId { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string CallbackBaseUrl { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int Timeout { get; set; } = 30;
}

public class SandboxSettings
{
    public bool IsEnabled { get; set; }
    public bool AutoSuccess { get; set; }
    public int DelaySeconds { get; set; } = 2;
}