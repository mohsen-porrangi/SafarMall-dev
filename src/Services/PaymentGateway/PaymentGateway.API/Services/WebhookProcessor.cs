using BuildingBlocks.Enums;
using PaymentGateway.API.Common;
using PaymentGateway.API.Common.Contracts;
using PaymentGateway.API.Data;
using PaymentGateway.API.Features.Command.ProcessWebhook;
using PaymentGateway.API.Models;
using System.Text.Json;

namespace PaymentGateway.API.Services;
public class WebhookProcessor(
    IPaymentGatewayFactory gatewayFactory,
    IUnitOfWork unitOfWork,
    ILogger<WebhookProcessor> logger) : IWebhookProcessor
{
    public async Task<ProcessWebhookResponse> ProcessWebhookAsync(
        PaymentGatewayType gatewayType,
        string requestBody,
        IDictionary<string, string> headers,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Processing webhook for gateway {Gateway}", gatewayType);

            // دریافت Provider
            var provider = gatewayFactory.GetProvider(gatewayType);

            // پردازش Webhook
            var isProcessed = await provider.ProcessWebhookAsync(requestBody, headers, cancellationToken);

            if (isProcessed)
            {
                // تجزیه محتوای webhook برای استخراج اطلاعات پرداخت
                var paymentInfo = await ExtractPaymentInfoFromWebhook(gatewayType, requestBody, cancellationToken);

                logger.LogInformation("Webhook processed successfully for gateway {Gateway}", gatewayType);

                return new ProcessWebhookResponse
                {
                    IsSuccessful = true,
                    PaymentId = paymentInfo.PaymentId,
                    NewStatus = paymentInfo.Status,
                    StatusCode = 200
                };
            }

            logger.LogWarning("Webhook processing failed for gateway {Gateway}", gatewayType);

            return new ProcessWebhookResponse
            {
                IsSuccessful = false,
                ErrorMessage = "Provider processing failed",
                StatusCode = 400
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing webhook for gateway {Gateway}", gatewayType);

            return new ProcessWebhookResponse
            {
                IsSuccessful = false,
                ErrorMessage = $"Processing error: {ex.Message}",
                StatusCode = 500
            };
        }
    }

    public async Task LogWebhookAsync(WebhookLog webhookLog, CancellationToken cancellationToken = default)
    {
        try
        {
            await unitOfWork.WebhookLogs.AddAsync(webhookLog, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogDebug("Webhook log saved: {GatewayType} - {EventType} - {IsProcessed}",
                webhookLog.GatewayType, webhookLog.EventType, webhookLog.IsProcessed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving webhook log for gateway {Gateway}", webhookLog.GatewayType);
            // در اینجا خطا را نمی‌اندازیم چون نباید پردازش webhook به خاطر مشکل logging متوقف شود
        }
    }

    public async Task CleanupOldLogsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-BusinessRules.Webhook.LogRetentionDays);

            await unitOfWork.WebhookLogs.DeleteOldLogsAsync(cutoffDate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Cleaned up webhook logs older than {CutoffDate}", cutoffDate);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cleaning up old webhook logs");
        }
    }

    /// <summary>
    /// استخراج اطلاعات پرداخت از محتوای webhook
    /// </summary>
    private async Task<(string? PaymentId, PaymentStatus? Status)> ExtractPaymentInfoFromWebhook(
        PaymentGatewayType gatewayType,
        string requestBody,
        CancellationToken cancellationToken)
    {
        try
        {
            return gatewayType switch
            {
                PaymentGatewayType.ZarinPal => await ExtractZarinPalInfo(requestBody, cancellationToken),
                PaymentGatewayType.Zibal => await ExtractZibalInfo(requestBody, cancellationToken),
                PaymentGatewayType.Sandbox => await ExtractSandboxInfo(requestBody, cancellationToken),
                _ => (null, null)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error extracting payment info from webhook");
            return (null, null);
        }
    }

    private async Task<(string? PaymentId, PaymentStatus? Status)> ExtractZarinPalInfo(
        string requestBody,
        CancellationToken cancellationToken)
    {
        try
        {
            var webhookData = JsonSerializer.Deserialize<Dictionary<string, object>>(requestBody);

            if (webhookData != null && webhookData.TryGetValue("Authority", out var authorityObj))
            {
                var authority = authorityObj.ToString();
                if (!string.IsNullOrEmpty(authority))
                {
                    // پیدا کردن پرداخت بر اساس GatewayReference
                    var payment = await unitOfWork.Payments.GetByGatewayReferenceAsync(authority, cancellationToken);
                    if (payment != null)
                    {
                        var status = DetermineStatusFromWebhook(webhookData);
                        return (payment.PaymentId, status);
                    }
                }
            }

            return (null, null);
        }
        catch
        {
            return (null, null);
        }
    }

    private async Task<(string? PaymentId, PaymentStatus? Status)> ExtractZibalInfo(
        string requestBody,
        CancellationToken cancellationToken)
    {
        try
        {
            var webhookData = JsonSerializer.Deserialize<Dictionary<string, object>>(requestBody);

            if (webhookData != null && webhookData.TryGetValue("trackId", out var trackIdObj))
            {
                var trackId = trackIdObj.ToString();
                if (!string.IsNullOrEmpty(trackId))
                {
                    // پیدا کردن پرداخت بر اساس GatewayReference
                    var payment = await unitOfWork.Payments.GetByGatewayReferenceAsync(trackId, cancellationToken);
                    if (payment != null)
                    {
                        var status = DetermineStatusFromWebhook(webhookData);
                        return (payment.PaymentId, status);
                    }
                }
            }

            return (null, null);
        }
        catch
        {
            return (null, null);
        }
    }

    private async Task<(string? PaymentId, PaymentStatus? Status)> ExtractSandboxInfo(
        string requestBody,
        CancellationToken cancellationToken)
    {
        try
        {
            var webhookData = JsonSerializer.Deserialize<Dictionary<string, object>>(requestBody);

            if (webhookData != null && webhookData.TryGetValue("reference", out var referenceObj))
            {
                var reference = referenceObj.ToString();
                if (!string.IsNullOrEmpty(reference))
                {
                    // پیدا کردن پرداخت بر اساس GatewayReference
                    var payment = await unitOfWork.Payments.GetByGatewayReferenceAsync(reference, cancellationToken);
                    if (payment != null)
                    {
                        var status = DetermineStatusFromWebhook(webhookData);
                        return (payment.PaymentId, status);
                    }
                }
            }

            return (null, null);
        }
        catch
        {
            return (null, null);
        }
    }

    /// <summary>
    /// تعیین وضعیت پرداخت از محتوای webhook
    /// </summary>
    private static PaymentStatus? DetermineStatusFromWebhook(Dictionary<string, object> webhookData)
    {
        try
        {
            // بررسی فیلدهای مختلف برای تشخیص وضعیت
            if (webhookData.TryGetValue("status", out var statusObj))
            {
                var status = statusObj.ToString()?.ToLower();
                return status switch
                {
                    "success" or "paid" or "ok" => PaymentStatus.Paid,
                    "failed" or "error" or "nok" => PaymentStatus.Failed,
                    "cancelled" or "cancel" => PaymentStatus.Cancelled,
                    _ => PaymentStatus.Pending
                };
            }

            if (webhookData.TryGetValue("result", out var resultObj))
            {
                if (int.TryParse(resultObj.ToString(), out var result))
                {
                    return result switch
                    {
                        100 => PaymentStatus.Paid, // ZarinPal/Zibal success code
                        101 => PaymentStatus.Paid, // Already verified
                        _ => PaymentStatus.Failed
                    };
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}