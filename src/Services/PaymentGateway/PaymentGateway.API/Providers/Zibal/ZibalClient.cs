using System.Text;
using System.Text.Json;

namespace PaymentGateway.API.Providers.Zibal;

/// <summary>
/// کلاینت HTTP برای زیبال
/// </summary>
public class ZibalClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ZibalClient> _logger;
    private readonly string _baseUrl;

    public ZibalClient(HttpClient httpClient, ILogger<ZibalClient> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["PaymentGateways:Zibal:BaseUrl"] ?? "https://gateway.zibal.ir/v1";
    }

    /// <summary>
    /// ایجاد درخواست پرداخت
    /// </summary>
    public async Task<ZibalCreateResponse?> CreatePaymentAsync(
        ZibalCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Creating Zibal payment for amount {Amount}", request.Amount);

            var response = await _httpClient.PostAsync($"{_baseUrl}/request", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("Zibal create response: {Response}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Zibal API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return null;
            }

            return JsonSerializer.Deserialize<ZibalCreateResponse>(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Zibal create payment API");
            return null;
        }
    }

    /// <summary>
    /// تایید پرداخت
    /// </summary>
    public async Task<ZibalVerifyResponse?> VerifyPaymentAsync(
        ZibalVerifyRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Verifying Zibal payment with trackId {TrackId}", request.TrackId);

            var response = await _httpClient.PostAsync($"{_baseUrl}/verify", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("Zibal verify response: {Response}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Zibal verify API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return null;
            }

            return JsonSerializer.Deserialize<ZibalVerifyResponse>(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Zibal verify payment API");
            return null;
        }
    }

    /// <summary>
    /// بررسی وضعیت پرداخت
    /// </summary>
    public async Task<ZibalVerifyResponse?> GetPaymentStatusAsync(
        long trackId,
        string merchant,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ZibalVerifyRequest
            {
                Merchant = merchant,
                TrackId = trackId
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Getting Zibal payment status for trackId {TrackId}", trackId);

            var response = await _httpClient.PostAsync($"{_baseUrl}/inquiry", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("Zibal status response: {Response}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Zibal status API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return null;
            }

            return JsonSerializer.Deserialize<ZibalVerifyResponse>(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Zibal status API");
            return null;
        }
    }
}