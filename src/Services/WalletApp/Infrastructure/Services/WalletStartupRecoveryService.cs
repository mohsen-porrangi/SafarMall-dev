using BuildingBlocks.Contracts.Services;
using BuildingBlocks.Messaging.Contracts;
using BuildingBlocks.Messaging.Events.UserEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WalletApp.Application.Common.Interfaces;
using WalletApp.Domain.Common.Contracts;

namespace WalletApp.Infrastructure.Services;

/// <summary>
/// Service to recover missing wallets on application startup
/// </summary>
public class WalletStartupRecoveryService(
        IServiceProvider serviceProvider,
        ILogger<WalletStartupRecoveryService> logger,
        IUserManagementService userManagement
    ) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Wallet Startup Recovery initiated");

        try
        {
            using var scope = serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            // Check for missing wallets
            var usersWithoutWallet = await GetUsersWithoutWallet(unitOfWork, userManagement, cancellationToken);

            if (usersWithoutWallet.Any())
            {
                logger.LogWarning("🔧 Found {Count} users without wallet during startup", usersWithoutWallet.Count);

                foreach (var userId in usersWithoutWallet)
                {
                    await messageBus.PublishAsync(new CreateWalletRetryEvent(userId), cancellationToken);
                }

                logger.LogInformation("Recovery events sent for {Count} missing wallets", usersWithoutWallet.Count);
            }
            else
            {
                logger.LogInformation("No missing wallets found during startup");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, " Error during wallet startup recovery");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task<List<Guid>> GetUsersWithoutWallet(IUnitOfWork unitOfWork, IUserManagementService userManagement, CancellationToken cancellationToken)
    {
        // Same logic as reconciliation service
        var allUserIds = await userManagement.GetUserAllIdsAsync(cancellationToken);
        var userIdsWithWallet = await unitOfWork.Wallets.GetUserIdsWithWalletAsync(cancellationToken);

        return allUserIds.Except(userIdsWithWallet).ToList();
    }
}