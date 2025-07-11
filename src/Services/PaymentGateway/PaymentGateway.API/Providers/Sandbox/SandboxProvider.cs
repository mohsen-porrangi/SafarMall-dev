using BuildingBlocks.Enums;
using PaymentGateway.API.Models;
using static PaymentGateway.API.Providers.Sandbox.SandboxModels;

namespace PaymentGateway.API.Providers.Sandbox;

/// <summary>
/// درگاه تستی Sandbox
/// </summary>
public class SandboxProvider : IPaymentProvider
{
    private readonly ILogger<SandboxProvider> _logger;
    private readonly IConfiguration _configuration;

    // ذخیره موقت پرداختها (در محیط واقعی از Cache/Database استفاده کنید)
    private static readonly Dictionary<string, SandboxPaymentData> _payments = new();

    public PaymentGatewayType GatewayType => PaymentGatewayType.Sandbox;

    public SandboxProvider(ILogger<SandboxProvider> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<CreatePaymentResult> CreatePaymentAsync(
        decimal amount,
        string description,        
        string? orderId = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken); // شبیه‌سازی تاخیر شبکه

        var authority = Guid.NewGuid().ToString();
        var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:7241";
        var paymentUrl = $"{baseUrl}/sandbox/payment/{authority}";

        // ذخیره اطلاعات پرداخت
        _payments[authority] = new SandboxPaymentData
        {
            Authority = authority,
            Amount = amount,
            Description = description,            
            OrderId = orderId,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Sandbox payment created: Authority={Authority}, Amount={Amount}",
            authority, amount);

        return new CreatePaymentResult
        {
            IsSuccessful = true,
            GatewayReference = authority,
            PaymentUrl = paymentUrl
        };
    }

    public async Task<VerifyPaymentResult> VerifyPaymentAsync(
        string gatewayReference,
        decimal expectedAmount,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(300, cancellationToken); // شبیه‌سازی تاخیر شبکه

        if (!_payments.TryGetValue(gatewayReference, out var payment))
        {
            return new VerifyPaymentResult
            {
                IsSuccessful = false,
                IsVerified = false,
                ErrorMessage = "پرداخت یافت نشد",
                ErrorCode = "NOT_FOUND"
            };
        }

        // بررسی مبلغ
        if (Math.Abs(payment.Amount - expectedAmount) > 0.01m)
        {
            return new VerifyPaymentResult
            {
                IsSuccessful = false,
                IsVerified = false,
                ErrorMessage = "مبلغ مطابقت ندارد",
                ErrorCode = "AMOUNT_MISMATCH"
            };
        }

        // بررسی وضعیت
        if (payment.Status != PaymentStatus.Paid)
        {
            return new VerifyPaymentResult
            {
                IsSuccessful = false,
                IsVerified = false,
                ErrorMessage = payment.Status == PaymentStatus.Cancelled ? "پرداخت لغو شده" : "پرداخت تکمیل نشده",
                ErrorCode = payment.Status.ToString().ToUpperInvariant()
            };
        }

        var transactionId = Random.Shared.Next(100000, 999999).ToString();
        var trackingCode = Random.Shared.Next(1000000, 9999999).ToString();

        // ذخیره اطلاعات تایید
        payment.RefId = transactionId;
        payment.TrackingCode = trackingCode;

        _logger.LogInformation("Sandbox payment verified: Authority={Authority}, TransactionId={TransactionId}",
            gatewayReference, transactionId);

        return new VerifyPaymentResult
        {
            IsSuccessful = true,
            IsVerified = true,
            TransactionId = transactionId,
            TrackingCode = trackingCode,
            ActualAmount = payment.Amount,
            VerificationDate = DateTime.UtcNow
        };
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(
        string gatewayReference,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);

        if (!_payments.TryGetValue(gatewayReference, out var payment))
        {
            return new PaymentStatusResult
            {
                IsSuccessful = false,
                Status = PaymentStatus.Failed,
                ErrorMessage = "پرداخت یافت نشد"
            };
        }

        return new PaymentStatusResult
        {
            IsSuccessful = true,
            Status = payment.Status,
            Amount = payment.Amount,
            TransactionId = payment.RefId,
            TrackingCode = payment.TrackingCode,
            CreatedAt = payment.CreatedAt,
            PaidAt = payment.PaidAt
        };
    }

    public async Task<bool> ProcessWebhookAsync(
        string requestBody,
        IDictionary<string, string> headers,
        CancellationToken cancellationToken = default)
    {
        // Sandbox webhook processing (اختیاری)
        await Task.Delay(50, cancellationToken);

        _logger.LogInformation("Sandbox webhook processed: {Body}", requestBody);
        return true;
    }

    /// <summary>
    /// شبیه‌سازی تکمیل پرداخت (برای UI تستی)
    /// </summary>
    public bool CompletePayment(string authority, bool success = true)
    {
        if (!_payments.TryGetValue(authority, out var payment))
        {
            return false;
        }

        payment.Status = success ? PaymentStatus.Paid : PaymentStatus.Failed;
        payment.PaidAt = success ? DateTime.UtcNow : null;

        if (success)
        {
            payment.RefId = Random.Shared.Next(100000, 999999).ToString();
            payment.TrackingCode = Random.Shared.Next(1000000, 9999999).ToString();
        }

        _logger.LogInformation("Sandbox payment {Status}: Authority={Authority}",
            payment.Status, authority);

        return true;
    }

    /// <summary>
    /// دریافت اطلاعات پرداخت (برای UI تستی)
    /// </summary>
    public SandboxPaymentData? GetPaymentData(string authority)
    {
        return _payments.TryGetValue(authority, out var payment) ? payment : null;
    }

    /// <summary>
    /// لغو پرداخت (برای UI تستی)
    /// </summary>
    public bool CancelPayment(string authority)
    {
        if (!_payments.TryGetValue(authority, out var payment))
        {
            return false;
        }

        payment.Status = PaymentStatus.Cancelled;
        _logger.LogInformation("Sandbox payment cancelled: Authority={Authority}", authority);
        return true;
    }
}