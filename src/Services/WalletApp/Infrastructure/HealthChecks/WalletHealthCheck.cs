using BuildingBlocks.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using WalletApp.Application.Common.Interfaces;
using WalletApp.Domain.Common.Contracts;

namespace WalletApp.Infrastructure.HealthChecks;

/// <summary>
/// Health check to monitor wallet creation status
/// </summary>
public class WalletHealthCheck(
    IServiceProvider serviceProvider,
    ILogger<WalletHealthCheck> logger,
    IUserManagementService userManagement) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Check for missing wallets
            var missingWalletsCount = await GetMissingWalletsCount(unitOfWork, userManagement, cancellationToken);

            if (missingWalletsCount == 0)
            {
                return HealthCheckResult.Healthy("All users have wallets");
            }

            if (missingWalletsCount <= 5) // Threshold
            {
                return HealthCheckResult.Degraded($"{missingWalletsCount} users without wallet");
            }

            return HealthCheckResult.Unhealthy($"{missingWalletsCount} users without wallet - threshold exceeded");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during wallet health check");
            return HealthCheckResult.Unhealthy("Wallet health check failed", ex);
        }
    }

    private async Task<int> GetMissingWalletsCount(IUnitOfWork unitOfWork, IUserManagementService userManagementClient, CancellationToken cancellationToken)
    {
        var allUserIds = await userManagementClient.GetUserAllIdsAsync(cancellationToken);
        var userIdsWithWallet = await unitOfWork.Wallets.GetUserIdsWithWalletAsync(cancellationToken);

        return allUserIds.Except(userIdsWithWallet).Count();
    }
}