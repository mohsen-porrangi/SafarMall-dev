using BuildingBlocks.Contracts.Options;
using BuildingBlocks.Enums;
using Microsoft.Extensions.Options;
using PaymentGateway.API.Models;
using System.Text.Json;

namespace PaymentGateway.API.Providers.Zibal;

/// <summary>
/// ارائه‌دهنده درگاه زیبال
/// </summary>
public class ZibalProvider(
    ZibalClient client,
    ILogger<ZibalProvider> logger,
    IOptions<PaymentGatewayOptions> options) : IPaymentProvider
{
    private readonly PaymentGatewayOptions _options = options.Value;

    public PaymentGatewayType GatewayType => PaymentGatewayType.Zibal;

    private string merchant => _options.Zibal.MerchantId;    
    private string callbackUrl => _options.CallbackBaseUrl + _options.Zibal.CallbackUrl;
    private string basePaymentUrl =>  _options.Zibal.BasePaymentUrl;

    public async Task<CreatePaymentResult> CreatePaymentAsync(
        decimal amount,
        string description,        
        string? orderId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ZibalCreateRequest
            {
                Merchant = merchant,
                Amount = (long)amount, // زیبال به ریال کار می‌کند
                Description = description,
                CallbackUrl = callbackUrl,
                OrderId = orderId
            };

            var response = await client.CreatePaymentAsync(request, cancellationToken);

            if (response == null)
            {
                return new CreatePaymentResult
                {
                    IsSuccessful = false,
                    ErrorMessage = "خطا در ارتباط با درگاه زیبال",
                    CallbackUrl = callbackUrl,
                    ErrorCode = "ZIBAL_CONNECTION_ERROR"
                };
            }
            if (response.Result == 103 & response.Message == "authentication error")
            {
                return new CreatePaymentResult
                {
                    IsSuccessful = false,
                    ErrorMessage = "خطا در اعتباری سنجی درگاه زیبال",
                    CallbackUrl = callbackUrl,
                    ErrorCode = "ZIBAL_AUTHENTICATION_ERROR"
                };
            }

            if (response.Result == ZibalResultCodes.Success)
            {
                var paymentUrl = $"{basePaymentUrl}/{response.TrackId}";

                return new CreatePaymentResult
                {
                    IsSuccessful = true,
                    GatewayReference = response.TrackId.ToString(),
                    CallbackUrl = callbackUrl,
                    PaymentUrl = paymentUrl
                };
            }

            return new CreatePaymentResult
            {
                IsSuccessful = false,
                ErrorMessage = ZibalResultCodes.GetMessage(response.Result),
                CallbackUrl = callbackUrl,
                ErrorCode = $"ZIBAL_{response.Result}"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating Zibal payment");
            return new CreatePaymentResult
            {
                IsSuccessful = false,
                ErrorMessage = "خطای داخلی در ایجاد پرداخت",
                CallbackUrl = callbackUrl,
                ErrorCode = "ZIBAL_INTERNAL_ERROR"
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
            if (!long.TryParse(gatewayReference, out var trackId))
            {
                return new VerifyPaymentResult
                {
                    IsSuccessful = false,
                    IsVerified = false,
                    ErrorMessage = "شناسه مرجع نامعتبر",
                    ErrorCode = "ZIBAL_INVALID_REFERENCE"
                };
            }

            var request = new ZibalVerifyRequest
            {
                Merchant = merchant,
                TrackId = trackId
            };

            var response = await client.VerifyPaymentAsync(request, cancellationToken);

            if (response == null)
            {
                return new VerifyPaymentResult
                {
                    IsSuccessful = false,
                    IsVerified = false,
                    ErrorMessage = "خطا در ارتباط با درگاه زیبال",
                    ErrorCode = "ZIBAL_CONNECTION_ERROR"
                };
            }

            if (response.Result == ZibalResultCodes.Success || response.Result == ZibalResultCodes.AlreadyVerified)
            {
                // بررسی مبلغ
                if (response.Amount != (long)expectedAmount)
                {
                    logger.LogWarning("Amount mismatch in Zibal verification. Expected: {Expected}, Actual: {Actual}",
                        expectedAmount, response.Amount);

                    return new VerifyPaymentResult
                    {
                        IsSuccessful = false,
                        IsVerified = false,
                        ErrorMessage = "مبلغ پرداخت با مبلغ درخواستی مطابقت ندارد",
                        ErrorCode = "ZIBAL_AMOUNT_MISMATCH"
                    };
                }

                return new VerifyPaymentResult
                {
                    IsSuccessful = true,
                    IsVerified = true,
                    TransactionId = response?.RefNumber?.ToString(),
                    TrackingCode = trackId.ToString(),
                    ActualAmount = response?.Amount,
                    VerificationDate = DateTime.UtcNow
                };
            }

            return new VerifyPaymentResult
            {
                IsSuccessful = false,
                IsVerified = false,
                ErrorMessage = ZibalResultCodes.GetMessage(response.Result),
                ErrorCode = $"ZIBAL_{response.Result}"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying Zibal payment");
            return new VerifyPaymentResult
            {
                IsSuccessful = false,
                IsVerified = false,
                ErrorMessage = "خطای داخلی در تایید پرداخت",
                ErrorCode = "ZIBAL_INTERNAL_ERROR"
            };
        }
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(
        string gatewayReference,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!long.TryParse(gatewayReference, out var trackId))
            {
                return new PaymentStatusResult
                {
                    IsSuccessful = false,
                    Status = PaymentStatus.Failed,
                    ErrorMessage = "شناسه مرجع نامعتبر"
                };
            }

            var response = await client.GetPaymentStatusAsync(trackId, merchant, cancellationToken);

            if (response == null)
            {
                return new PaymentStatusResult
                {
                    IsSuccessful = false,
                    Status = PaymentStatus.Failed,
                    ErrorMessage = "خطا در ارتباط با درگاه زیبال"
                };
            }

            var status = response.Result switch
            {
                ZibalResultCodes.Success => PaymentStatus.Paid,
                ZibalResultCodes.AlreadyVerified => PaymentStatus.Paid,
                ZibalResultCodes.TransactionNotFound => PaymentStatus.Failed,
                _ => PaymentStatus.Failed
            };

            return new PaymentStatusResult
            {
                IsSuccessful = true,
                Status = status,
                Amount = response.Amount,
                TransactionId = response.RefNumber.ToString(),
                TrackingCode = trackId.ToString()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting Zibal payment status");
            return new PaymentStatusResult
            {
                IsSuccessful = false,
                Status = PaymentStatus.Failed,
                ErrorMessage = "خطای داخلی در بررسی وضعیت"
            };
        }
    }

    public async Task<bool> ProcessWebhookAsync(
        string requestBody,
        IDictionary<string, string> headers,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Processing Zibal webhook");

            // زیبال معمولاً webhook ندارد، اما برای آینده آماده می‌کنیم
            var webhookData = JsonSerializer.Deserialize<Dictionary<string, object>>(requestBody);

            if (webhookData == null)
            {
                logger.LogWarning("Invalid Zibal webhook data");
                return false;
            }

            logger.LogInformation("Zibal webhook processed successfully");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Zibal webhook");
            return false;
        }
    }
}