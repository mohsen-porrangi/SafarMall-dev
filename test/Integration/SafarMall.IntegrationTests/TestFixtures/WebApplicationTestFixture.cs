using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SafarMall.IntegrationTests.Configuration;
using System.Diagnostics;
using Xunit;

namespace SafarMall.IntegrationTests.TestFixtures;

/// <summary>
/// Test fixture for managing external service dependencies and test environment setup
/// </summary>
public class WebApplicationTestFixture : IAsyncLifetime
{
    private readonly List<Process> _serviceProcesses = new();
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebApplicationTestFixture> _logger;

    public WebApplicationTestFixture()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

        var serviceProvider = serviceCollection.BuildServiceProvider();
        _logger = serviceProvider.GetRequiredService<ILogger<WebApplicationTestFixture>>();

        _httpClient = new HttpClient();
        _httpClient.Timeout = TestConfiguration.Timeouts.LongTimeout;
    }

    /// <summary>
    /// Initialize test environment - start all required services
    /// </summary>
    public async Task InitializeAsync()
    {
        _logger.LogInformation("Starting integration test environment initialization...");

        try
        {
            // Check if services are already running
            var servicesRunning = await CheckServicesHealthAsync();

            if (!servicesRunning)
            {
                _logger.LogInformation("Services not detected, attempting to start them...");
                await StartServicesAsync();

                // Wait for services to be ready
                await WaitForServicesReadyAsync();
            }
            else
            {
                _logger.LogInformation("Services are already running");
            }

            _logger.LogInformation("Integration test environment initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize test environment");
            throw;
        }
    }

    /// <summary>
    /// Cleanup test environment
    /// </summary>
    public async Task DisposeAsync()
    {
        _logger.LogInformation("Cleaning up integration test environment...");

        try
        {
            // Stop services if we started them
            await StopServicesAsync();

            _httpClient.Dispose();

            _logger.LogInformation("Integration test environment cleaned up successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during test environment cleanup");
        }
    }

    /// <summary>
    /// Check if all required services are running and healthy
    /// </summary>
    private async Task<bool> CheckServicesHealthAsync()
    {
        var services = new[]
        {
            TestConfiguration.ServiceUrls.UserManagement,
            TestConfiguration.ServiceUrls.Wallet,
            TestConfiguration.ServiceUrls.Order
        };

        foreach (var serviceUrl in services)
        {
            try
            {
                var healthUrl = $"{serviceUrl}/health";
                var response = await _httpClient.GetAsync(healthUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Service at {ServiceUrl} is not healthy: {StatusCode}", serviceUrl, response.StatusCode);
                    return false;
                }

                _logger.LogDebug("Service at {ServiceUrl} is healthy", serviceUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check health for service at {ServiceUrl}", serviceUrl);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Start all required services
    /// Note: This assumes services can be started programmatically or are already configured to run
    /// In a real environment, you might use Docker Compose, Kubernetes, or other orchestration
    /// </summary>
    private async Task StartServicesAsync()
    {
        _logger.LogInformation("Starting services...");

        // In a real scenario, you would:
        // 1. Start services using Docker Compose
        // 2. Use TestContainers to manage service containers
        // 3. Start services programmatically if they support it
        // 4. Or ensure services are running externally before tests

        // For this example, we'll assume services are started externally
        // and just wait for them to be available

        await Task.Delay(1000); // Simulate startup time
        _logger.LogInformation("Services startup initiated");
    }

    /// <summary>
    /// Wait for all services to be ready and responding
    /// </summary>
    private async Task WaitForServicesReadyAsync()
    {
        _logger.LogInformation("Waiting for services to be ready...");

        var maxWaitTime = TestConfiguration.Timeouts.LongTimeout;
        var startTime = DateTime.UtcNow;
        var checkInterval = TimeSpan.FromSeconds(5);

        while (DateTime.UtcNow - startTime < maxWaitTime)
        {
            if (await CheckServicesHealthAsync())
            {
                _logger.LogInformation("All services are ready");
                return;
            }

            _logger.LogDebug("Services not ready yet, waiting {Interval} seconds...", checkInterval.TotalSeconds);
            await Task.Delay(checkInterval);
        }

        throw new TimeoutException($"Services did not become ready within {maxWaitTime.TotalMinutes} minutes");
    }

    /// <summary>
    /// Stop services that were started by this fixture
    /// </summary>
    private async Task StopServicesAsync()
    {
        _logger.LogInformation("Stopping services...");

        foreach (var process in _serviceProcesses)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    await process.WaitForExitAsync();
                }
                process.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping service process {ProcessName}", process.ProcessName);
            }
        }

        _serviceProcesses.Clear();
        _logger.LogInformation("Services stopped");
    }

    /// <summary>
    /// Get a configured HTTP client for testing
    /// </summary>
    public HttpClient CreateTestHttpClient()
    {
        var client = new HttpClient();
        client.Timeout = TestConfiguration.Timeouts.DefaultTimeout;
        client.DefaultRequestHeaders.Add("User-Agent", "SafarMall-IntegrationTests/1.0");
        client.DefaultRequestHeaders.Add("X-Test-Environment", "true");

        return client;
    }

    /// <summary>
    /// Reset test environment state between tests if needed
    /// </summary>
    public async Task ResetEnvironmentAsync()
    {
        _logger.LogDebug("Resetting test environment state...");

        // Here you could:
        // 1. Clear test databases
        // 2. Reset service states
        // 3. Clear caches
        // 4. etc.

        await Task.CompletedTask;

        _logger.LogDebug("Test environment state reset completed");
    }

    /// <summary>
    /// Check if a specific service is running
    /// </summary>
    public async Task<bool> IsServiceRunningAsync(string serviceUrl)
    {
        try
        {
            var healthUrl = $"{serviceUrl}/health";
            var response = await _httpClient.GetAsync(healthUrl);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Wait for a specific service to be ready
    /// </summary>
    public async Task WaitForServiceAsync(string serviceUrl, TimeSpan? timeout = null)
    {
        timeout ??= TestConfiguration.Timeouts.DefaultTimeout;
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            if (await IsServiceRunningAsync(serviceUrl))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new TimeoutException($"Service at {serviceUrl} did not become ready within {timeout.Value.TotalSeconds} seconds");
    }
}