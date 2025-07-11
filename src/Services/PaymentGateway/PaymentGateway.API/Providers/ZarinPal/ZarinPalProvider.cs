using Azure.Core;
using BuildingBlocks.Contracts.Options;
using BuildingBlocks.Enums;
using Microsoft.Extensions.Options;
using PaymentGateway.API.Models;
using PaymentGateway.API.Providers.Zibal;
using System.Text.Json;

namespace PaymentGateway.API.Providers.ZarinPal;

/// <summary>
/// ارائه‌دهنده درگاه ZarinPal
/// </summary>
public class ZarinPalProvider(
    ZarinPalClient client,
    ILogger<ZibalProvider> logger,
    IOptions<PaymentGatewayOptions> options) : IPaymentProvider
{
    private readonly PaymentGatewayOptions _options = options.Value;

    public PaymentGatewayType GatewayType => PaymentGatewayType.ZarinPal;
    private string merchant => _options.ZarinPal.MerchantId;
    private string basePaymentUrl => _options.Zibal.BasePaymentUrl;
    private string callbackUrl => _options.CallbackBaseUrl + _options.ZarinPal.CallbackUrl;

    public async Task<CreatePaymentResult> CreatePaymentAsync(
        decimal amount,
        string description,        
        string? orderId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {                        

            var request = new ZarinPalCreateRequest
            {
                MerchantId = merchant,
                Amount = (long)amount,
                Description = description,
                CallbackUrl = callbackUrl,
                Metadata = new ZarinPalMetadata { OrderId = orderId ?? "" }
            };
            var response = await client.CreatePaymentAsync(request, cancellationToken);   

            if (response?.Data?.Code == ZarinPalStatusCodes.Success)
            {               
                if (response == null)
                {
                    var paymentUrl = $"{basePaymentUrl}/{response?.Data.Authority}";
                    return new CreatePaymentResult
                    {
                        IsSuccessful = true,
                        GatewayReference = response?.Data.Authority,
                        PaymentUrl = paymentUrl
                    };
                }
            }

            var errorMessage = response?.Errors?.FirstOrDefault()?.Message ??
                              ZarinPalStatusCodes.GetMessage(response?.Data?.Code ?? 0);

            return new CreatePaymentResult
            {
                IsSuccessful = false,
                ErrorMessage = errorMessage,
                ErrorCode = response?.Data?.Code.ToString()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating ZarinPal payment");
            return new CreatePaymentResult
            {
                IsSuccessful = false,
                ErrorMessage = "خطا در ارتباط با درگاه پرداخت",
                ErrorCode = "GATEWAY_ERROR"
            };
        }
    }

    public async Task<VerifyPaymentResult> VerifyPaymentAsync(
        string gatewayReference,
        decimal expectedAmount,
        CancellationToken cancellationToken = default)
    {
        try
        {

            var request = new ZarinPalVerifyRequest
            {
                MerchantId = merchant,
                Amount = (long)expectedAmount,
                Authority = gatewayReference
            };

            var response = await client.VerifyPaymentAsync(request, cancellationToken);
            if (response?.Data?.Code == ZarinPalStatusCodes.Success)
            {
                return new VerifyPaymentResult
                {
                    IsSuccessful = true,
                    IsVerified = true,
                    TransactionId = response.Data.RefId.ToString(),
                    TrackingCode = response.Data.RefId.ToString(),
                    ActualAmount = expectedAmount,
                    VerificationDate = DateTime.UtcNow
                };
            }

            return new VerifyPaymentResult
            {
                IsSuccessful = false,
                IsVerified = false,
                ErrorMessage = ZarinPalStatusCodes.GetMessage(response?.Data?.Code ?? 0),
                ErrorCode = response?.Data?.Code.ToString()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying ZarinPal payment: {Authority}", gatewayReference);
            return new VerifyPaymentResult
            {
                IsSuccessful = false,
                IsVerified = false,
                ErrorMessage = "خطا در تایید پرداخت",
                ErrorCode = "VERIFICATION_ERROR"
            };
        }
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(
        string gatewayReference,
        CancellationToken cancellationToken = default)
    {
        // ZarinPal doesn't have dedicated status API, we use verify result to determine status
        try
        {
            var verifyResult = await VerifyPaymentAsync(gatewayReference, 0, cancellationToken);

            return new PaymentStatusResult
            {
                IsSuccessful = verifyResult.IsSuccessful,
                Status = verifyResult.IsVerified ? PaymentStatus.Paid : PaymentStatus.Failed,
                TransactionId = verifyResult.TransactionId,
                TrackingCode = verifyResult.TrackingCode,
                ErrorMessage = verifyResult.ErrorMessage
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting ZarinPal payment status: {Authority}", gatewayReference);
            return new PaymentStatusResult
            {
                IsSuccessful = false,
                Status = PaymentStatus.Failed,
                ErrorMessage = "خطا در دریافت وضعیت پرداخت"
            };
        }
    }

    public async Task<bool> ProcessWebhookAsync(
        string requestBody,
        IDictionary<string, string> headers,
        CancellationToken cancellationToken = default)
    {
        // ZarinPal معمولاً از webhook استفاده نمی‌کند
        // این برای سازگاری آینده است
        await Task.CompletedTask;
        return true;
    }
}