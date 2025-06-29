using PaymentGateway.API.Models;
using System.Text.Json;

namespace PaymentGateway.API.Providers.Zibal;

/// <summary>
/// ارائه‌دهنده درگاه زیبال
/// </summary>
public class ZibalProvider : IPaymentProvider
{
    private readonly ZibalClient _client;
    private readonly ILogger<ZibalProvider> _logger;
    private readonly string _merchant;
    private readonly string _gatewayUrl;

    public PaymentGatewayType GatewayType => PaymentGatewayType.Zibal;

    public ZibalProvider(ZibalClient client, ILogger<ZibalProvider> logger, IConfiguration configuration)
    {
        _client = client;
        _logger = logger;
        _merchant = configuration["PaymentGateways:Zibal:Merchant"] ?? throw new ArgumentException("Zibal merchant not configured");
        _gatewayUrl = configuration["PaymentGateways:Zibal:GatewayUrl"] ?? "https://gateway.zibal.ir/start";
    }

    public async Task<CreatePaymentResult> CreatePaymentAsync(
        decimal amount,
        string description,
        string callbackUrl,
        string? orderId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ZibalCreateRequest
            {
                Merchant = _merchant,
                Amount = (long)amount, // زیبال به ریال کار می‌کند
                Description = description,
                CallbackUrl = callbackUrl,
                OrderId = orderId
            };

            var response = await _client.CreatePaymentAsync(request, cancellationToken);

            if (response == null)
            {
                return new CreatePaymentResult
                {
                    IsSuccessful = false,
                    ErrorMessage = "خطا در ارتباط با درگاه زیبال",
                    ErrorCode = "ZIBAL_CONNECTION_ERROR"
                };
            }

            if (response.Result == ZibalResultCodes.Success)
            {
                var paymentUrl = $"{_gatewayUrl}/{response.TrackId}";

                return new CreatePaymentResult
                {
                    IsSuccessful = true,
                    GatewayReference = response.TrackId.ToString(),
                    PaymentUrl = paymentUrl
                };
            }

            return new CreatePaymentResult
            {
                IsSuccessful = false,
                ErrorMessage = ZibalResultCodes.GetMessage(response.Result),
                ErrorCode = $"ZIBAL_{response.Result}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Zibal payment");
            return new CreatePaymentResult
            {
                IsSuccessful = false,
                ErrorMessage = "خطای داخلی در ایجاد پرداخت",
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
                Merchant = _merchant,
                TrackId = trackId
            };

            var response = await _client.VerifyPaymentAsync(request, cancellationToken);

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
                    _logger.LogWarning("Amount mismatch in Zibal verification. Expected: {Expected}, Actual: {Actual}",
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
                    TransactionId = response.RefNumber.ToString(),
                    TrackingCode = trackId.ToString(),
                    ActualAmount = response.Amount,
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
            _logger.LogError(ex, "Error verifying Zibal payment");
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

            var response = await _client.GetPaymentStatusAsync(trackId, _merchant, cancellationToken);

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
            _logger.LogError(ex, "Error getting Zibal payment status");
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
            _logger.LogInformation("Processing Zibal webhook");

            // زیبال معمولاً webhook ندارد، اما برای آینده آماده می‌کنیم
            var webhookData = JsonSerializer.Deserialize<Dictionary<string, object>>(requestBody);

            if (webhookData == null)
            {
                _logger.LogWarning("Invalid Zibal webhook data");
                return false;
            }

            _logger.LogInformation("Zibal webhook processed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Zibal webhook");
            return false;
        }
    }
}