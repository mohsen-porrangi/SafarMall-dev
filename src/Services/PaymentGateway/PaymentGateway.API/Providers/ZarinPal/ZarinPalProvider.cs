using Microsoft.Extensions.Options;
using PaymentGateway.API.Models;
using System.Text.Json;

namespace PaymentGateway.API.Providers.ZarinPal;

/// <summary>
/// تنظیمات ZarinPal
/// </summary>
public class ZarinPalOptions
{
    public const string SectionName = "PaymentGateways:ZarinPal";

    public string MerchantId { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.zarinpal.com/pg/v4/payment/";
    public string SandboxBaseUrl { get; set; } = "https://sandbox.zarinpal.com/pg/v4/payment/";
    public bool UseSandbox { get; set; } = false;
}

/// <summary>
/// ارائه‌دهنده درگاه ZarinPal
/// </summary>
public class ZarinPalProvider : IPaymentProvider
{
    private readonly HttpClient _httpClient;
    private readonly ZarinPalOptions _options;
    private readonly ILogger<ZarinPalProvider> _logger;

    public PaymentGatewayType GatewayType => PaymentGatewayType.ZarinPal;

    public ZarinPalProvider(
        HttpClient httpClient,
        IOptions<ZarinPalOptions> options,
        ILogger<ZarinPalProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CreatePaymentResult> CreatePaymentAsync(
        decimal amount,
        string description,
        string callbackUrl,
        string? orderId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = _options.UseSandbox ? _options.SandboxBaseUrl : _options.BaseUrl;
            var requestUrl = $"{baseUrl}request.json";

            var requestData = new ZarinPalCreateRequest
            {
                MerchantId = _options.MerchantId,
                Amount = (long)amount,
                Description = description,
                CallbackUrl = callbackUrl,
                Metadata = new ZarinPalMetadata { OrderId = orderId ?? "" }
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _logger.LogInformation("Creating ZarinPal payment for amount: {Amount}", amount);

            var response = await _httpClient.PostAsync(requestUrl, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            var responseData = JsonSerializer.Deserialize<ZarinPalCreateResponse>(responseContent);

            if (responseData?.Data?.Code == ZarinPalStatusCodes.Success)
            {
                var paymentUrl = _options.UseSandbox
                    ? $"https://sandbox.zarinpal.com/pg/StartPay/{responseData.Data.Authority}"
                    : $"https://www.zarinpal.com/pg/StartPay/{responseData.Data.Authority}";

                return new CreatePaymentResult
                {
                    IsSuccessful = true,
                    GatewayReference = responseData.Data.Authority,
                    PaymentUrl = paymentUrl
                };
            }

            var errorMessage = responseData?.Errors?.FirstOrDefault()?.Message ??
                              ZarinPalStatusCodes.GetMessage(responseData?.Data?.Code ?? 0);

            return new CreatePaymentResult
            {
                IsSuccessful = false,
                ErrorMessage = errorMessage,
                ErrorCode = responseData?.Data?.Code.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ZarinPal payment");
            return new CreatePaymentResult
            {
                IsSuccessful = false,
                ErrorMessage = "خطا در ارتباط با درگاه پرداخت",
                ErrorCode = "GATEWAY_ERROR"
            };
        }
    }

    public async Task<VerifyPaymentResult> VerifyPaymentAsync(
        string gatewayReference,
        decimal expectedAmount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = _options.UseSandbox ? _options.SandboxBaseUrl : _options.BaseUrl;
            var requestUrl = $"{baseUrl}verify.json";

            var requestData = new ZarinPalVerifyRequest
            {
                MerchantId = _options.MerchantId,
                Amount = (long)expectedAmount,
                Authority = gatewayReference
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _logger.LogInformation("Verifying ZarinPal payment: {Authority}", gatewayReference);

            var response = await _httpClient.PostAsync(requestUrl, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            var responseData = JsonSerializer.Deserialize<ZarinPalVerifyResponse>(responseContent);

            if (responseData?.Data?.Code == ZarinPalStatusCodes.Success)
            {
                return new VerifyPaymentResult
                {
                    IsSuccessful = true,
                    IsVerified = true,
                    TransactionId = responseData.Data.RefId.ToString(),
                    TrackingCode = responseData.Data.RefId.ToString(),
                    ActualAmount = expectedAmount,
                    VerificationDate = DateTime.UtcNow
                };
            }

            return new VerifyPaymentResult
            {
                IsSuccessful = false,
                IsVerified = false,
                ErrorMessage = ZarinPalStatusCodes.GetMessage(responseData?.Data?.Code ?? 0),
                ErrorCode = responseData?.Data?.Code.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying ZarinPal payment: {Authority}", gatewayReference);
            return new VerifyPaymentResult
            {
                IsSuccessful = false,
                IsVerified = false,
                ErrorMessage = "خطا در تایید پرداخت",
                ErrorCode = "VERIFICATION_ERROR"
            };
        }
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(
        string gatewayReference,
        CancellationToken cancellationToken = default)
    {
        // ZarinPal doesn't have dedicated status API, we use verify result to determine status
        try
        {
            var verifyResult = await VerifyPaymentAsync(gatewayReference, 0, cancellationToken);

            return new PaymentStatusResult
            {
                IsSuccessful = verifyResult.IsSuccessful,
                Status = verifyResult.IsVerified ? PaymentStatus.Paid : PaymentStatus.Failed,
                TransactionId = verifyResult.TransactionId,
                TrackingCode = verifyResult.TrackingCode,
                ErrorMessage = verifyResult.ErrorMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ZarinPal payment status: {Authority}", gatewayReference);
            return new PaymentStatusResult
            {
                IsSuccessful = false,
                Status = PaymentStatus.Failed,
                ErrorMessage = "خطا در دریافت وضعیت پرداخت"
            };
        }
    }

    public async Task<bool> ProcessWebhookAsync(
        string requestBody,
        IDictionary<string, string> headers,
        CancellationToken cancellationToken = default)
    {
        // ZarinPal معمولاً از webhook استفاده نمی‌کند
        // این برای سازگاری آینده است
        await Task.CompletedTask;
        return true;
    }
}