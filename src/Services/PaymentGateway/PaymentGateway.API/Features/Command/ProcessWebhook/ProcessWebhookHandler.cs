using BuildingBlocks.CQRS;
using BuildingBlocks.Enums;
using PaymentGateway.API.Common;
using PaymentGateway.API.Models;
using PaymentGateway.API.Services;
using System.Text.Json;

namespace PaymentGateway.API.Features.Command.ProcessWebhook;

/// <summary>
/// پردازشگر Webhook
/// </summary>
public class ProcessWebhookHandler : ICommandHandler<ProcessWebhookCommand, ProcessWebhookResponse>
{
    private readonly IWebhookProcessor _webhookProcessor;
    private readonly ILogger<ProcessWebhookHandler> _logger;

    public ProcessWebhookHandler(
        IWebhookProcessor webhookProcessor,
        ILogger<ProcessWebhookHandler> logger)
    {
        _webhookProcessor = webhookProcessor;
        _logger = logger;
    }

    public async Task<ProcessWebhookResponse> Handle(
        ProcessWebhookCommand request,
        CancellationToken cancellationToken)
    {
        var webhookLog = new WebhookLog
        {
            GatewayType = request.GatewayType,
            RequestBody = request.RequestBody,
            RequestHeaders = JsonSerializer.Serialize(request.Headers),
            SourceIp = request.SourceIp
        };

        try
        {
            _logger.LogInformation("Processing webhook from {Gateway} - IP: {IP}",
                request.GatewayType, request.SourceIp);

            // بررسی IP مجاز
            if (!BusinessRules.Webhook.IsAllowedIP(request.SourceIp, request.GatewayType))
            {
                _logger.LogWarning("Webhook from unauthorized IP: {IP} for gateway: {Gateway}",
                    request.SourceIp, request.GatewayType);

                webhookLog.MarkAsError("Unauthorized IP address", 403);
                await _webhookProcessor.LogWebhookAsync(webhookLog, cancellationToken);

                return new ProcessWebhookResponse
                {
                    IsSuccessful = false,
                    ErrorMessage = "Unauthorized IP address",
                    StatusCode = 403
                };
            }

            // بررسی اندازه محتوا
            if (request.RequestBody.Length > BusinessRules.Webhook.MaxContentSize)
            {
                _logger.LogWarning("Webhook content too large: {Size} bytes", request.RequestBody.Length);

                webhookLog.MarkAsError("Content too large", 413);
                await _webhookProcessor.LogWebhookAsync(webhookLog, cancellationToken);

                return new ProcessWebhookResponse
                {
                    IsSuccessful = false,
                    ErrorMessage = "Content too large",
                    StatusCode = 413
                };
            }

            // پردازش Webhook
            var result = await _webhookProcessor.ProcessWebhookAsync(
                request.GatewayType,
                request.RequestBody,
                request.Headers,
                cancellationToken);

            if (result.IsSuccessful)
            {
                webhookLog.EventType = DetermineEventType(result.NewStatus);
                webhookLog.PaymentId = result.PaymentId;
                webhookLog.MarkAsProcessed();

                _logger.LogInformation("Webhook processed successfully for PaymentId: {PaymentId}",
                    result.PaymentId);
            }
            else
            {
                webhookLog.MarkAsError(result.ErrorMessage ?? "Processing failed", result.StatusCode);

                _logger.LogWarning("Webhook processing failed: {Error}", result.ErrorMessage);
            }

            await _webhookProcessor.LogWebhookAsync(webhookLog, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook from {Gateway}", request.GatewayType);

            webhookLog.MarkAsError($"Internal error: {ex.Message}", 500);
            await _webhookProcessor.LogWebhookAsync(webhookLog, cancellationToken);

            return new ProcessWebhookResponse
            {
                IsSuccessful = false,
                ErrorMessage = "Internal server error",
                StatusCode = 500
            };
        }
    }

    private static WebhookEventType DetermineEventType(PaymentStatus? status)
    {
        return status switch
        {
            PaymentStatus.Paid => WebhookEventType.PaymentCompleted,
            PaymentStatus.Failed => WebhookEventType.PaymentFailed,
            PaymentStatus.Cancelled => WebhookEventType.PaymentCancelled,
            _ => WebhookEventType.PaymentCreated
        };
    }
}