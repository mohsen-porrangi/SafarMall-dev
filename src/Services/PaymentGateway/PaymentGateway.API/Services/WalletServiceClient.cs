using System.Text.Json;

namespace PaymentGateway.API.Services;

/// <summary>
/// کلاینت ارتباط با سرویس کیف پول
/// </summary>
public interface IWalletServiceClient
{
    /// <summary>
    /// اطلاع‌رسانی تکمیل پرداخت به سرویس کیف پول
    /// </summary>
    Task<bool> NotifyPaymentCompletedAsync(
        string authority,
        string status,
        decimal? amount = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// بررسی وجود تراکنش در سرویس کیف پول
    /// </summary>
    Task<bool> CheckTransactionExistsAsync(
        string authority,
        CancellationToken cancellationToken = default);
}

public class WalletServiceClient : IWalletServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WalletServiceClient> _logger;

    public WalletServiceClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<WalletServiceClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        // تنظیم Base Address
        var walletServiceUrl = _configuration["Services:WalletService:BaseUrl"];
        if (!string.IsNullOrEmpty(walletServiceUrl))
        {
            _httpClient.BaseAddress = new Uri(walletServiceUrl);
        }

        // تنظیم Timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<bool> NotifyPaymentCompletedAsync(
        string authority,
        string status,
        decimal? amount = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                Authority = authority,
                Status = status,
                Amount = amount
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "/api/transactions/payment-callback",
                content,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Payment callback sent successfully for authority: {Authority}", authority);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Payment callback failed for authority: {Authority}. Status: {Status}, Error: {Error}",
                    authority, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error when sending payment callback for authority: {Authority}", authority);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout when sending payment callback for authority: {Authority}", authority);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when sending payment callback for authority: {Authority}", authority);
            return false;
        }
    }

    public async Task<bool> CheckTransactionExistsAsync(
        string authority,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/internal/transactions/by-reference/{authority}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
            else
            {
                _logger.LogWarning("Error checking transaction existence for authority: {Authority}. Status: {Status}",
                    authority, response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking transaction existence for authority: {Authority}", authority);
            return false;
        }
    }
}