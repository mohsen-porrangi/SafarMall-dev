using BuildingBlocks.Enums;
using Carter;
using MediatR;
using PaymentGateway.API.Features.Command.ProcessWebhook;
using PaymentGateway.API.Models;

namespace PaymentGateway.API.Features.Endpoints;

/// <summary>
/// Endpoint پردازش Webhook
/// </summary>
public class WebhookEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        // ZarinPal Webhook
        app.MapPost("/api/webhooks/zarinpal", async (
            HttpContext context,
            IMediator mediator,
            CancellationToken cancellationToken
            ) =>
        {
            return await ProcessWebhookAsync(
                   context,
                   mediator,
                   PaymentGatewayType.ZarinPal,
                   cancellationToken);
        })
            .WithName("ZarinPalWebhook")
            .WithTags("Webhooks")
            .AllowAnonymous()
            .WithSummary("ZarinPal webhook handler")
            .WithDescription("مدیریت اعلان‌های پرداخت از درگاه زرین‌پال");

        // Zibal Webhook
        app.MapPost("/api/webhooks/zibal", async (
            HttpContext context,
            IMediator mediator,
            CancellationToken cancellationToken
            ) =>
        {
            return await ProcessWebhookAsync(
                   context,
                   mediator,
                   PaymentGatewayType.Zibal,
                   cancellationToken);
        })
            .WithName("ZibalWebhook")
            .WithTags("Webhooks")
            .AllowAnonymous()
            .WithSummary("Zibal webhook handler")
            .WithDescription("مدیریت اعلان‌های پرداخت از درگاه زیبال");

        // Sandbox Webhook (for testing)
        app.MapPost("/api/webhooks/sandbox", async (
            HttpContext context,
            IMediator mediator,
            CancellationToken cancellationToken
            ) =>
        {
            return await ProcessWebhookAsync(
                   context,
                   mediator,
                   PaymentGatewayType.Sandbox,
                   cancellationToken);
        })
            .WithName("SandboxWebhook")
            .WithTags("Webhooks")
            .AllowAnonymous().
             WithSummary("Sandbox webhook handler")
            .WithDescription("مدیریت اعلان‌های پرداخت از درگاه تستی");

        // Generic webhook endpoint
        app.MapPost("/api/webhooks/{gateway}", async (
            string gateway,
            HttpContext context,
            IMediator mediator,
            CancellationToken cancellationToken
            ) =>
        {
            if (!Enum.TryParse<PaymentGatewayType>(gateway, true, out var gatewayType))
            {
                return Results.BadRequest(new { error = "Invalid gateway type" });
            }

            return await ProcessWebhookAsync(
                context,
                mediator,
                gatewayType,
                cancellationToken);
        })
            .WithName("GenericWebhook")
            .WithTags("Webhooks")
            .AllowAnonymous()
            .WithSummary("Generic webhook handler")
            .WithDescription("اعلان‌های پرداخت را از هر درگاه پشتیبانی‌شده‌ای مدیریت کنید");
    }   
    private static async Task<IResult> ProcessWebhookAsync(
        HttpContext context,
        IMediator mediator,
        PaymentGatewayType gatewayType,
        CancellationToken cancellationToken)
    {
        try
        {
            // خواندن محتوای درخواست
            using var reader = new StreamReader(context.Request.Body);
            var requestBody = await reader.ReadToEndAsync(cancellationToken);

            // استخراج هدرها
            var headers = context.Request.Headers
                .ToDictionary(h => h.Key, h => string.Join(",", h.Value.AsEnumerable()));

            // استخراج IP
            var sourceIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = context.Request.Headers.UserAgent.ToString();

            var command = new ProcessWebhookCommand
            {
                GatewayType = gatewayType,
                RequestBody = requestBody,
                Headers = headers,
                SourceIp = sourceIp,
                UserAgent = userAgent
            };

            var result = await mediator.Send(command, cancellationToken);

            if (result.IsSuccessful)
            {
                return Results.Ok(new
                {
                    success = true,
                    message = "Webhook processed successfully",
                    paymentId = result.PaymentId,
                    status = result.NewStatus?.ToString()
                });
            }

            return Results.StatusCode(result.StatusCode);
        }
        catch (Exception ex)
        {
            // Log the exception but don't expose details to webhook sender
            return Results.StatusCode(500);
        }
    }
}