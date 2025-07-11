using BuildingBlocks.Contracts.Options;
using BuildingBlocks.Enums;
using BuildingBlocks.Services;
using BuildingBlocks.ValueObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WalletApp.Application.Common.Interfaces;

namespace WalletApp.Infrastructure.Services;

public sealed class PaymentGatewayClient(
    HttpClient httpClient,
    ILogger<PaymentGatewayClient> logger,
    IOptions<PaymentGatewayServiceOptions> options,
    IHttpContextAccessor httpContextAccessor)
    : AuthorizedHttpClient(httpClient, logger, httpContextAccessor), IPaymentGatewayClient
{
    private readonly PaymentGatewayServiceOptions _options = options.Value;

    public async Task<PaymentResult> CreatePaymentAsync(
        Money amount,
        string description,        
        PaymentGatewayType paymentGateway,
        string? orderId = null,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Creating payment: Amount={Amount}, Gateway={Gateway}", amount, paymentGateway);

        var request = new CreatePaymentRequest(
            Amount: amount.Value,
            Currency: CurrencyCode.IRR,
            Description: description,            
            PaymentGateway: paymentGateway,
            OrderId: orderId);

        try
        {
       //     var fullUrl = $"{_options.BaseUrl}{_options.Endpoints.CreatePayment}";
            var response = await PostAsync<CreatePaymentRequest, CreatePaymentResponse>(
                _options.Endpoints.CreatePayment,
                request,
                cancellationToken);

            return new PaymentResult
            {
                IsSuccessful = response?.Success ?? false,
                Authority = response?.PaymentId,
                PaymentUrl = response?.PaymentUrl,
                ErrorMessage = response?.Error,
                ErrorCode = response?.ErrorCode
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create payment");
            return new PaymentResult
            {
                IsSuccessful = false,
                ErrorMessage = "خطا در ایجاد درخواست پرداخت",
                ErrorCode = "GATEWAY_CONNECTION_ERROR"
            };
        }
    }

    public async Task<PaymentVerificationResult> VerifyPaymentAsync(
        string authority,
        Money expectedAmount,
        PaymentGatewayType gateway = PaymentGatewayType.ZarinPal,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Verifying payment: Authority={Authority}", authority);

        var request = new VerifyPaymentRequest(
            GatewayReference: authority,
            Status: "OK",
            Amount: expectedAmount.Value,
            GatewayType: gateway);

        try
        {
            var response = await PostAsync<VerifyPaymentRequest, VerifyPaymentResponse>(
                _options.Endpoints.VerifyPayment,
                request,
                cancellationToken);

            return new PaymentVerificationResult
            {
                IsSuccessful = response?.Success ?? false,
                IsVerified = response?.Verified ?? false,
                ReferenceId = response?.TransactionId,
                ActualAmount = response?.Amount != null ? Money.FromIrr(response.Amount.Value) : null,
                VerificationDate = response?.VerificationDate,
                ErrorMessage = response?.Error,
                ErrorCode = response?.ErrorCode
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to verify payment");
            return new PaymentVerificationResult
            {
                IsSuccessful = false,
                IsVerified = false,
                ErrorMessage = "خطا در تایید پرداخت",
                ErrorCode = "VERIFICATION_CONNECTION_ERROR"
            };
        }
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(
        string authority,
        PaymentGatewayType gateway = PaymentGatewayType.ZarinPal,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Getting payment status: Authority={Authority}", authority);

        try
        {
            var endpoint = _options.Endpoints.GetPaymentStatus.Replace("{paymentId}", authority);
            var response = await GetAsync<PaymentStatusResponse>(endpoint, cancellationToken);

            return new PaymentStatusResult
            {
                IsSuccessful = response?.Success ?? false,
                Status = response?.Status ?? PaymentStatus.Failed,
                Amount = response?.Amount != null ? Money.FromIrr(response.Amount.Value) : null,
                CreatedAt = response?.CreatedAt,
                PaidAt = response?.PaidAt,
                ErrorMessage = response?.ErrorMessage
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get payment status");
            return new PaymentStatusResult
            {
                IsSuccessful = false,
                Status = PaymentStatus.Failed,
                ErrorMessage = "خطا در دریافت وضعیت پرداخت"
            };
        }
    }
}

// Request/Response Models
internal sealed record CreatePaymentRequest(
    decimal Amount,
    CurrencyCode Currency,
    string Description,    
    PaymentGatewayType PaymentGateway,
    string? OrderId);

internal sealed record CreatePaymentResponse(
    bool Success,
    string? PaymentId,
    string? PaymentUrl,
    string? Error,
    string? ErrorCode);

internal sealed record VerifyPaymentRequest(
    string GatewayReference,
    string Status,
    decimal Amount,
    PaymentGatewayType GatewayType);

internal sealed record VerifyPaymentResponse(
    bool Success,
    bool Verified,
    string? TransactionId,
    decimal? Amount,
    DateTime? VerificationDate,
    string? Error,
    string? ErrorCode);

internal sealed record PaymentStatusResponse(
    bool Success,
    PaymentStatus Status,
    decimal? Amount,
    DateTime? CreatedAt,
    DateTime? PaidAt,
    string? ErrorMessage);