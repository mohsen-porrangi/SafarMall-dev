using BuildingBlocks.Enums;
using PaymentGateway.API.Models;

namespace PaymentGateway.API.BackgroundServices;

/// <summary>
/// سرویس بررسی وضعیت پرداخت‌های معلق
/// </summary>
public class PaymentStatusCheckService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentStatusCheckService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5); // هر 5 دقیقه

    public PaymentStatusCheckService(
        IServiceProvider serviceProvider,
        ILogger<PaymentStatusCheckService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment Status Check Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckExpiredPaymentsAsync(stoppingToken);
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Payment Status Check Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Payment Status Check Service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // تاخیر کوتاه برای خطا
            }
        }
    }

    private async Task CheckExpiredPaymentsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<Data.IUnitOfWork>();

        try
        {
            // پیدا کردن پرداخت‌های منقضی شده
            var expiredPayments = await unitOfWork.Payments.FindAsync(
                p => p.Status == PaymentStatus.Pending &&
                     p.ExpiresAt < DateTime.UtcNow &&
                     !p.IsDeleted,
                cancellationToken: cancellationToken);

            var expiredList = expiredPayments.ToList();
            if (expiredList.Count == 0)
            {
                _logger.LogDebug("No expired payments found");
                return;
            }

            _logger.LogInformation("Found {Count} expired payments", expiredList.Count);

            foreach (var payment in expiredList)
            {
                payment.Status = PaymentStatus.Expired;
                payment.ErrorMessage = "Payment expired";
                unitOfWork.Payments.Update(payment);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Marked {Count} payments as expired", expiredList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking expired payments");
        }
    }
}