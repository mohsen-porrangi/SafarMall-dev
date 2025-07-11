using PaymentGateway.API.Services;

namespace PaymentGateway.API.BackgroundServices;

/// <summary>
/// سرویس تلاش مجدد برای پرداخت‌های ناموفق
/// </summary>
public class RetryFailedPaymentsService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RetryFailedPaymentsService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(10); // هر 10 دقیقه

    public RetryFailedPaymentsService(
        IServiceProvider serviceProvider,
        ILogger<RetryFailedPaymentsService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Retry Failed Payments Service started");

        // تاخیر اولیه برای اطمینان از شروع سایر سرویس‌ها
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRetryablePaymentsAsync(stoppingToken);
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Retry Failed Payments Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Retry Failed Payments Service");
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); // تاخیر کوتاه برای خطا
            }
        }
    }

    private async Task ProcessRetryablePaymentsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var retryService = scope.ServiceProvider.GetRequiredService<IRetryService>();

        try
        {
            _logger.LogDebug("Processing retryable payments");
            await retryService.ProcessPendingPaymentsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing retryable payments");
        }
    }
}