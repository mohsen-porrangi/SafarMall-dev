using BuildingBlocks.Contracts.Options;
using BuildingBlocks.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WalletApp.Application.Common.Interfaces;

namespace WalletApp.Infrastructure.ExternalServices;

public sealed class OrderServiceClient(
        HttpClient httpClient,
        ILogger<OrderServiceClient> logger,
        IHttpContextAccessor httpContextAccessor,
        IOptions<OrderServiceOptions> options
    ) : AuthorizedHttpClient(httpClient, logger, httpContextAccessor), IOrderServiceClient
{
    private readonly OrderServiceOptions _options = options.Value;
    public async Task<bool> CompleteOrderAsync(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation("Completing order: {OrderId}", orderId);

            var request = new CompleteOrderRequest();
            var endpoint = _options.Endpoints.CompeleteOrder.Replace("{orderId}", orderId.ToString());
            await PostAsync<CompleteOrderRequest>(endpoint, request, cancellationToken);
                
                //($"/api/internal/orders/{orderId}/complete", request, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to complete order: {OrderId}", orderId);
            return false;
        }
    }
}