using BuildingBlocks.Contracts.Services;
using Microsoft.Extensions.Logging;
using Order.Infrastructure.ExternalServices.Common;

namespace Order.Infrastructure.ExternalServices.Wallet;

public class WalletServiceClient(HttpClient httpClient, ILogger<WalletServiceClient> logger)
    : BaseHttpClient(httpClient, logger), IWalletService
{
    public async Task<bool> CreateWalletAsync(Guid userId, CancellationToken cancellationToken)
    {
        var response = await PostAsync<CreateWalletRequest, CreateWalletResponse>(
            "/api/internal/wallet",
            new CreateWalletRequest(userId),
            cancellationToken);

        return response?.Success ?? false;
    }

    private record CreateWalletRequest(Guid UserId);
    private record CreateWalletResponse(bool Success);
}