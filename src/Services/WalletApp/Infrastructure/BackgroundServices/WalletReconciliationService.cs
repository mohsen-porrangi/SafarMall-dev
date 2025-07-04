using BuildingBlocks.Contracts.Services;
using BuildingBlocks.Messaging.Contracts;
using BuildingBlocks.Messaging.Events.UserEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WalletApp.Application.Common.Interfaces;
using WalletApp.Domain.Common.Contracts;

namespace WalletApp.Infrastructure.BackgroundServices;

/// <summary>
/// Background service to check and fix missing wallets
/// </summary>
public class WalletReconciliationService(
        IServiceProvider serviceProvider,
        ILogger<WalletReconciliationService> logger,
        IUserManagementService userManagement
    ) : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Wallet Reconciliation Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndCreateMissingWallets(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Wallet Reconciliation Service cancelled");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in Wallet Reconciliation Service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait before retry
            }
        }
    }

    private async Task CheckAndCreateMissingWallets(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        try
        {
            logger.LogDebug("Checking for missing wallets...");

            // پیدا کردن یوزرهایی که wallet ندارن
            var usersWithoutWallet = await GetUsersWithoutWallet(unitOfWork, userManagement, cancellationToken);

            if (usersWithoutWallet.Any())
            {
                logger.LogWarning("Found {Count} users without wallet", usersWithoutWallet.Count);

                foreach (var userId in usersWithoutWallet)
                {
                    try
                    {
                        logger.LogInformation("Creating missing wallet for user: {UserId}", userId);

                        // Re-send event for missing wallet
                        await messageBus.PublishAsync(new CreateWalletRetryEvent(userId), cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to send retry event for user: {UserId}", userId);
                    }
                }
            }
            else
            {
                logger.LogDebug("No missing wallets found");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during wallet reconciliation check");
        }
    }

    private async Task<List<Guid>> GetUsersWithoutWallet(IUnitOfWork unitOfWork, IUserManagementService userManagement, CancellationToken cancellationToken)
    {
        // این query باید در repository layer پیاده شه
        // فعلاً یک نمونه ساده:
        var allUserIds = await userManagement.GetUserAllIdsAsync(cancellationToken);
        var userIdsWithWallet = await unitOfWork.Wallets.GetUserIdsWithWalletAsync(cancellationToken);

        return allUserIds.Except(userIdsWithWallet).ToList();
    }
}