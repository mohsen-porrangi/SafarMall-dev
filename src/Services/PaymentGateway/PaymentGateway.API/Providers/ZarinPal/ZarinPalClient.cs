using System.Text;
using System.Text.Json;

namespace PaymentGateway.API.Providers.ZarinPal;

/// <summary>
/// کلاینت HTTP برای زرین‌پال
/// </summary>
public class ZarinPalClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ZarinPalClient> _logger;
    private readonly string _baseUrl;

    public ZarinPalClient(HttpClient httpClient, ILogger<ZarinPalClient> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["PaymentGateways:ZarinPal:BaseUrl"] ?? "https://api.zarinpal.com/pg/v4/payment";

        // تنظیم User-Agent
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "PaymentGateway/1.0");
    }

    /// <summary>
    /// ایجاد درخواست پرداخت
    /// </summary>
    public async Task<ZarinPalCreateResponse?> CreatePaymentAsync(
        ZarinPalCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Creating ZarinPal payment for amount {Amount}", request.Amount);

            var response = await _httpClient.PostAsync($"{_baseUrl}/request.json", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("ZarinPal create response: {Response}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("ZarinPal API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return null;
            }

            return JsonSerializer.Deserialize<ZarinPalCreateResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ZarinPal create payment API");
            return null;
        }
    }

    /// <summary>
    /// تایید پرداخت
    /// </summary>
    public async Task<ZarinPalVerifyResponse?> VerifyPaymentAsync(
        ZarinPalVerifyRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Verifying ZarinPal payment with authority {Authority}", request.Authority);

            var response = await _httpClient.PostAsync($"{_baseUrl}/verify.json", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("ZarinPal verify response: {Response}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("ZarinPal verify API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return null;
            }

            return JsonSerializer.Deserialize<ZarinPalVerifyResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ZarinPal verify payment API");
            return null;
        }
    }

    /// <summary>
    /// بررسی وضعیت پرداخت (استفاده از همان API تایید)
    /// </summary>
    public async Task<ZarinPalVerifyResponse?> GetPaymentStatusAsync(
        string authority,
        string merchantId,
        long amount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ZarinPalVerifyRequest
            {
                MerchantId = merchantId,
                Authority = authority,
                Amount = amount
            };

            _logger.LogInformation("Getting ZarinPal payment status for authority {Authority}", authority);

            // زرین‌پال API جداگانه برای status ندارد، از verify استفاده می‌کنیم
            return await VerifyPaymentAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ZarinPal payment status");
            return null;
        }
    }
}