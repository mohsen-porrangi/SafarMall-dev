using BuildingBlocks.Enums;
using PaymentGateway.API.Common;
using PaymentGateway.API.Models;
using System.Security.Cryptography;
using System.Text;

namespace PaymentGateway.API.Middleware;

/// <summary>
/// Middleware برای تایید امضای Webhook ها
/// </summary>
public class WebhookSignatureMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WebhookSignatureMiddleware> _logger;
    private readonly IConfiguration _configuration;

    public WebhookSignatureMiddleware(
        RequestDelegate next,
        ILogger<WebhookSignatureMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // فقط برای endpoint های webhook
        if (!context.Request.Path.StartsWithSegments("/api/webhook"))
        {
            await _next(context);
            return;
        }

        // بررسی فعال بودن تایید امضا
        var requireSignature = _configuration.GetValue<bool>("Webhook:RequireSignature");
        if (!requireSignature)
        {
            await _next(context);
            return;
        }

        try
        {
            var isValid = await ValidateWebhookSignatureAsync(context);
            if (!isValid)
            {
                _logger.LogWarning("Invalid webhook signature from IP: {IP}",
                    context.GetClientIpAddress());

                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid signature");
                return;
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating webhook signature");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Signature validation error");
        }
    }

    private async Task<bool> ValidateWebhookSignatureAsync(HttpContext context)
    {
        // خواندن محتوای درخواست
        context.Request.EnableBuffering();
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body.Position = 0;

        if (string.IsNullOrEmpty(body))
        {
            return false;
        }

        // دریافت امضا از header
        var signatureHeader = _configuration["Webhook:SignatureHeader"] ?? "X-Signature";
        var signature = context.Request.Headers[signatureHeader].FirstOrDefault();

        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("Missing signature header: {Header}", signatureHeader);
            return false;
        }

        // تشخیص نوع درگاه از URL یا header
        var gatewayType = DetermineGatewayType(context);
        if (!gatewayType.HasValue)
        {
            _logger.LogWarning("Cannot determine gateway type for webhook validation");
            return false;
        }

        // تایید امضا بر اساس نوع درگاه
        return gatewayType.Value switch
        {
            PaymentGatewayType.ZarinPal => ValidateZarinPalSignature(body, signature),
            PaymentGatewayType.Zibal => ValidateZibalSignature(body, signature),
            PaymentGatewayType.Sandbox => true, // Sandbox همیشه معتبر
            _ => false
        };
    }

    private PaymentGatewayType? DetermineGatewayType(HttpContext context)
    {
        // از URL path
        if (context.Request.Path.ToString().Contains("zarinpal", StringComparison.OrdinalIgnoreCase))
            return PaymentGatewayType.ZarinPal;

        if (context.Request.Path.ToString().Contains("zibal", StringComparison.OrdinalIgnoreCase))
            return PaymentGatewayType.Zibal;

        if (context.Request.Path.ToString().Contains("sandbox", StringComparison.OrdinalIgnoreCase))
            return PaymentGatewayType.Sandbox;

        // از header
        var gatewayHeader = context.Request.Headers["X-Gateway-Type"].FirstOrDefault();
        if (Enum.TryParse<PaymentGatewayType>(gatewayHeader, true, out var gateway))
            return gateway;

        return null;
    }

    private bool ValidateZarinPalSignature(string body, string signature)
    {
        try
        {
            var secret = _configuration["PaymentGateways:ZarinPal:WebhookSecret"];
            if (string.IsNullOrEmpty(secret))
            {
                _logger.LogWarning("ZarinPal webhook secret not configured");
                return true; // اگر secret تنظیم نشده، بپذیر
            }

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
            var computedSignature = Convert.ToHexString(computedHash).ToLower();

            var providedSignature = signature.Replace("sha256=", "").ToLower();
            return computedSignature == providedSignature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating ZarinPal signature");
            return false;
        }
    }

    private bool ValidateZibalSignature(string body, string signature)
    {
        try
        {
            var secret = _configuration["PaymentGateways:Zibal:WebhookSecret"];
            if (string.IsNullOrEmpty(secret))
            {
                _logger.LogWarning("Zibal webhook secret not configured");
                return true; // اگر secret تنظیم نشده، بپذیر
            }

            // Zibal از MD5 استفاده می‌کند
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(body + secret));
            var computedSignature = Convert.ToHexString(hash).ToLower();

            return computedSignature == signature.ToLower();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Zibal signature");
            return false;
        }
    }
}