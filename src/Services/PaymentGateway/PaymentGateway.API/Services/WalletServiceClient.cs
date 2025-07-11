using BuildingBlocks.Contracts.Options;
using BuildingBlocks.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;

namespace PaymentGateway.API.Services;

public sealed class WalletServiceClient(
        HttpClient httpClient,
        ILogger<WalletServiceClient> logger,
        IOptions<WalletServiceOptions> options,
        IHttpContextAccessor httpContextAccessor
        ) : AuthorizedHttpClient(httpClient, logger, httpContextAccessor) , IWalletServiceClient
{
    private readonly WalletServiceOptions _options = options.Value;
    public async Task<bool> ProcessPaymentCallbackAsync(
        PaymentCallbackRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation("Sending payment callback to Wallet: Authority={Authority}", request.Authority);

            await PostAsync<PaymentCallbackRequest>(_options.Endpoints.PaymentCallback , request, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send payment callback to Wallet Service");
            return false;
        }
    }
}