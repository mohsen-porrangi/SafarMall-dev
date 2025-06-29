using SafarMall.IntegrationTests.Configuration;
using SafarMall.IntegrationTests.Helpers;
using SafarMall.IntegrationTests.Models;

namespace SafarMall.IntegrationTests.TestFixtures;

public class WalletTestFixture : IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed = false;

    public WalletTestFixture()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TestConfiguration.Timeouts.DefaultTimeout;
        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "SafarMall-WalletTests/1.0");
    }

    /// <summary>
    /// Create wallet for user (internal endpoint)
    /// </summary>
    public async Task<bool> CreateWalletInternalAsync(Guid userId)
    {
        var request = new { UserId = userId };

        var response = await _httpClient.PostAsJsonAsync(
            EndpointUrls.Wallet.CreateWalletInternal,
            request);

        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Check if user has wallet (internal endpoint)
    /// </summary>
    public async Task<bool> CheckWalletExistsAsync(Guid userId)
    {
        var response = await _httpClient.GetAsync(
            EndpointUrls.Wallet.CheckWalletExists(userId));

        if (response.IsSuccessStatusCode)
        {
            var result = await response.ReadAsJsonAsync<dynamic>();
            return result?.hasWallet ?? false;
        }

        return false;
    }

    /// <summary>
    /// Get wallet balance internal (without authentication)
    /// </summary>
    public async Task<decimal> GetWalletBalanceInternalAsync(Guid userId)
    {
        var response = await _httpClient.GetAsync(
            EndpointUrls.Wallet.GetWalletBalanceInternal(userId));

        if (response.IsSuccessStatusCode)
        {
            var result = await response.ReadAsJsonAsync<dynamic>();
            return result?.totalBalanceInIrr ?? 0m;
        }

        return 0m;
    }

    /// <summary>
    /// Check affordability internal
    /// </summary>
    public async Task<bool> CheckAffordabilityAsync(Guid userId, decimal amount)
    {
        var request = new
        {
            Amount = amount,
            Currency = "IRR"
        };

        var response = await _httpClient.PostAsJsonAsync(
            EndpointUrls.Wallet.CheckAffordability(userId),
            request);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.ReadAsJsonAsync<dynamic>();
            return result?.canAfford ?? false;
        }

        return false;
    }

    /// <summary>
    /// Simulate successful payment gateway response
    /// </summary>
    public async Task<bool> SimulateSuccessfulPaymentAsync(string authority, decimal amount)
    {
        var callbackRequest = new
        {
            authority = authority,
            status = "OK",
            amount = amount
        };

        var response = await _httpClient.PostAsJsonAsync(
            EndpointUrls.Wallet.PaymentCallback,
            callbackRequest);

        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Simulate failed payment gateway response
    /// </summary>
    public async Task<bool> SimulateFailedPaymentAsync(string authority, decimal amount)
    {
        var callbackRequest = new
        {
            authority = authority,
            status = "FAILED",
            amount = amount
        };

        var response = await _httpClient.PostAsJsonAsync(
            EndpointUrls.Wallet.PaymentCallback,
            callbackRequest);

        // For failed payments, we expect either BadRequest or specific error handling
        return response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
               response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Wait for transaction to be processed
    /// </summary>
    public async Task WaitForTransactionProcessingAsync(Guid userId, decimal expectedBalance, TimeSpan? timeout = null)
    {
        timeout ??= TestConfiguration.Timeouts.DefaultTimeout;
        var endTime = DateTime.UtcNow.Add(timeout.Value);

        while (DateTime.UtcNow < endTime)
        {
            var currentBalance = await GetWalletBalanceInternalAsync(userId);
            if (Math.Abs(currentBalance - expectedBalance) < 0.01m) // Allow for small rounding differences
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException($"Transaction was not processed within {timeout.Value.TotalSeconds} seconds");
    }

    /// <summary>
    /// Generate test payment authority (for testing)
    /// </summary>
    public string GenerateTestAuthority()
    {
        return $"TEST-AUTH-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Perform complete deposit flow (initiate + callback)
    /// </summary>
    public async Task<bool> PerformCompleteDepositAsync(string userToken, decimal amount, string description = "Test deposit")
    {
        try
        {
            // Set authentication
            _httpClient.AddAuthorizationHeader(userToken);

            // Step 1: Initiate deposit
            var depositRequest = new
            {
                amount = amount,
                description = description,
                callbackUrl = "https://test.callback.url"
            };

            var depositResponse = await _httpClient.PostAsJsonAsync(
                EndpointUrls.Wallet.DirectDeposit,
                depositRequest);

            if (!depositResponse.IsSuccessStatusCode)
                return false;

            var depositResult = await depositResponse.ReadAsJsonAsync<DirectDepositResponse>();
            if (depositResult?.Authority == null)
                return false;

            // Step 2: Complete via callback
            await Task.Delay(500); // Small delay to simulate payment processing

            return await SimulateSuccessfulPaymentAsync(depositResult.Authority, amount);
        }
        finally
        {
            _httpClient.RemoveAuthorizationHeader();
        }
    }

    /// <summary>
    /// Setup test wallet with initial balance
    /// </summary>
    public async Task<bool> SetupTestWalletAsync(Guid userId, string userToken, decimal initialBalance = 0)
    {
        // Ensure wallet exists
        var walletExists = await CheckWalletExistsAsync(userId);
        if (!walletExists)
        {
            var created = await CreateWalletInternalAsync(userId);
            if (!created)
                return false;
        }

        // Add initial balance if requested
        if (initialBalance > 0)
        {
            return await PerformCompleteDepositAsync(userToken, initialBalance, "Initial test balance");
        }

        return true;
    }

    /// <summary>
    /// Cleanup test data
    /// </summary>
    public async Task CleanupTestDataAsync(Guid userId)
    {
        // In a real scenario, you might want to clean up test transactions
        // For now, we'll just verify the wallet state
        var exists = await CheckWalletExistsAsync(userId);
        if (exists)
        {
            // Could implement cleanup logic here if needed
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Validate wallet state
    /// </summary>
    public async Task<bool> ValidateWalletStateAsync(Guid userId, decimal expectedBalance)
    {
        var actualBalance = await GetWalletBalanceInternalAsync(userId);
        return Math.Abs(actualBalance - expectedBalance) < 0.01m;
    }

    /// <summary>
    /// Get wallet statistics for testing
    /// </summary>
    public async Task<WalletTestStats> GetWalletStatsAsync(Guid userId)
    {
        var balance = await GetWalletBalanceInternalAsync(userId);
        var exists = await CheckWalletExistsAsync(userId);

        return new WalletTestStats
        {
            UserId = userId,
            Balance = balance,
            WalletExists = exists,
            CanAfford1000 = await CheckAffordabilityAsync(userId, 1000m),
            CanAfford10000 = await CheckAffordabilityAsync(userId, 10000m),
            CheckedAt = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Wallet test statistics
/// </summary>
public class WalletTestStats
{
    public Guid UserId { get; set; }
    public decimal Balance { get; set; }
    public bool WalletExists { get; set; }
    public bool CanAfford1000 { get; set; }
    public bool CanAfford10000 { get; set; }
    public DateTime CheckedAt { get; set; }
}