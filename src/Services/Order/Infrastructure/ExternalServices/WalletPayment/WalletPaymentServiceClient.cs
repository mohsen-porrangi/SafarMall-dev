using BuildingBlocks.Contracts.Options;
using BuildingBlocks.Enums;
using BuildingBlocks.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Order.Application.Common.Interfaces;

namespace Order.Infrastructure.ExternalServices.WalletPayment;

/// <summary>
/// Wallet Service HTTP client implementation
/// Based on PaymentGatewayClient pattern with AuthorizedHttpClient
/// </summary>
public sealed class WalletServiceClient(
    HttpClient httpClient,
    ILogger<WalletServiceClient> logger,
    IOptions<WalletServiceOptions> options,
    IHttpContextAccessor httpContextAccessor)
    : AuthorizedHttpClient(httpClient, logger, httpContextAccessor), IWalletServiceClient
{
    private readonly WalletServiceOptions _options = options.Value;
    /// <summary>
    /// Process integrated purchase through Wallet Service
    /// </summary>
    public async Task<IntegratedPurchaseResult> IntegratedPurchaseAsync(
        IntegratedPurchaseRequest request,
        CancellationToken cancellationToken = default)
    {        
        Logger.LogInformation("Processing integrated purchase: UserId={UserId}, Amount={Amount}, OrderId={OrderId}",
            request.UserId, request.TotalAmount, request.OrderId);

        try
        {
            var walletRequest = new WalletIntegratedPurchaseRequest
            {
                UserId = request.UserId,
                TotalAmount = request.TotalAmount,
                Currency = request.Currency,
                PaymentGateway = request.PaymentGateway,
                OrderId = request.OrderId,
                Description = request.Description,
                UseCredit = request.UseCredit
            };

            var response = await PostAsync<WalletIntegratedPurchaseRequest, WalletIntegratedPurchaseResponse>(
                _options.Endpoints.IntegratedPurchase,
                walletRequest,
                cancellationToken);

            if (response == null)
            {
                Logger.LogError("Wallet service returned null response for integrated purchase");
                return CreateFailureResult("دریافت پاسخ از سرویس کیف پول ناموفق بود");
            }

            return MapToIntegratedPurchaseResult(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to process integrated purchase for user {UserId}", request.UserId);
            return CreateFailureResult("خطا در پردازش پرداخت");
        }
    }

    /// <summary>
    /// Check user affordability
    /// </summary>
    public async Task<AffordabilityResult> CheckAffordabilityAsync(
        Guid userId,
        decimal amount,
        CurrencyCode currency = CurrencyCode.IRR,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Checking affordability: UserId={UserId}, Amount={Amount}", userId, amount);

        try
        {
            var request = new CheckAffordabilityRequest(amount, currency.ToString());
            var endpoint = _options.Endpoints.CheckAffordability.Replace("{userId}", userId.ToString());

            var response = await PostAsync<CheckAffordabilityRequest, CheckAffordabilityResponse>(
                endpoint,
                request,
                cancellationToken);

            if (response == null)
            {
                Logger.LogError("Wallet service returned null response for affordability check");
                return new AffordabilityResult
                {
                    CanAfford = false,
                    Reason = "خطا در بررسی موجودی کیف پول"
                };
            }

            return new AffordabilityResult
            {
                CanAfford = response.CanAfford,
                Reason = response.Reason,
                AvailableBalance = response.AvailableBalance,
                RequiredAmount = response.RequiredAmount,
                Shortfall = response.Shortfall
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to check affordability for user {UserId}", userId);
            return new AffordabilityResult
            {
                CanAfford = false,
                Reason = "خطا در اتصال به سرویس کیف پول"
            };
        }
    }

    /// <summary>
    /// Get wallet status
    /// </summary>
    public async Task<WalletStatusResult> GetWalletStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Getting wallet status: UserId={UserId}", userId);

        try
        {
            var endpoint = _options.Endpoints.GetWalletStatus.Replace("{userId}", userId.ToString());

            var response = await GetAsync<WalletStatusResponse>(endpoint, cancellationToken);

            if (response == null)
            {
                Logger.LogError("Wallet service returned null response for wallet status");
                return new WalletStatusResult
                {
                    HasWallet = false,
                    IsActive = false,
                    CanMakePayment = false
                };
            }

            return new WalletStatusResult
            {
                HasWallet = response.HasWallet,
                IsActive = response.IsActive,
                TotalBalanceInIrr = response.TotalBalanceInIrr,
                CanMakePayment = response.CanMakePayment
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get wallet status for user {UserId}", userId);
            return new WalletStatusResult
            {
                HasWallet = false,
                IsActive = false,
                CanMakePayment = false
            };
        }
    }

    #region Private Methods

    /// <summary>
    /// Map wallet response to integrated purchase result
    /// </summary>
    private static IntegratedPurchaseResult MapToIntegratedPurchaseResult(WalletIntegratedPurchaseResponse response)
    {
        return new IntegratedPurchaseResult
        {
            IsSuccessful = response.IsSuccessful,
            WalletPurchaseType = response.PurchaseType,
            TotalAmount = response.TotalAmount,
            WalletBalance = response.WalletBalance,
            RequiredPayment = response.RequiredPayment,
            PurchaseTransactionId = response.PurchaseTransactionId,
            PaymentTransactionId = response.PaymentTransactionId,
            PaymentUrl = response.PaymentUrl,
            Authority = response.Authority,
            ErrorMessage = response.ErrorMessage,
            ProcessedAt = response.ProcessedAt
        };
    }

    /// <summary>
    /// Create failure result
    /// </summary>
    private static IntegratedPurchaseResult CreateFailureResult(string errorMessage)
    {
        return new IntegratedPurchaseResult
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage
        };
    }

    #endregion
}

#region Internal DTOs

/// <summary>
/// Internal wallet integrated purchase request
/// </summary>
internal record WalletIntegratedPurchaseRequest
{
    public Guid UserId { get; init; }
    public decimal TotalAmount { get; init; }
    public CurrencyCode Currency { get; init; } = CurrencyCode.IRR;
    public PaymentGatewayType PaymentGateway { get; init; }
    public Guid OrderId { get; init; } 
    public string Description { get; init; } = string.Empty;
    public bool UseCredit { get; init; } = false;
}

/// <summary>
/// Internal wallet integrated purchase response
/// </summary>
internal record WalletIntegratedPurchaseResponse
{
    public bool IsSuccessful { get; init; }
    public PurchaseType PurchaseType { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal WalletBalance { get; init; }
    public decimal RequiredPayment { get; init; }
    public Guid? PurchaseTransactionId { get; init; }
    public Guid? PaymentTransactionId { get; init; }
    public string? PaymentUrl { get; init; }
    public string? Authority { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime? ProcessedAt { get; init; }
}

/// <summary>
/// Check affordability internal request
/// </summary>
internal record CheckAffordabilityRequest(decimal Amount, string Currency = "IRR");

/// <summary>
/// Check affordability internal response
/// </summary>
internal record CheckAffordabilityResponse
{
    public bool CanAfford { get; init; }
    public string? Reason { get; init; }
    public decimal AvailableBalance { get; init; }
    public decimal RequiredAmount { get; init; }
    public decimal Shortfall { get; init; }
}

/// <summary>
/// Wallet status internal response
/// </summary>
internal record WalletStatusResponse
{
    public bool HasWallet { get; init; }
    public bool IsActive { get; init; }
    public decimal TotalBalanceInIrr { get; init; }
    public bool CanMakePayment { get; init; }
}

#endregion