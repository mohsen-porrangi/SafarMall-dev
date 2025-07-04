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
        app.MapPost("/api/webhooks/zarinpal", ProcessZarinPalWebhookAsync)
            .WithName("ZarinPalWebhook")
            .WithTags("Webhooks")
            .AllowAnonymous()
            .WithOpenApi(operation =>
            {
                operation.Summary = "ZarinPal webhook handler";
                operation.Description = "Handle payment notifications from ZarinPal gateway";
                return operation;
            });

        // Zibal Webhook
        app.MapPost("/api/webhooks/zibal", ProcessZibalWebhookAsync)
            .WithName("ZibalWebhook")
            .WithTags("Webhooks")
            .AllowAnonymous()
            .WithOpenApi(operation =>
            {
                operation.Summary = "Zibal webhook handler";
                operation.Description = "Handle payment notifications from Zibal gateway";
                return operation;
            });

        // Sandbox Webhook (for testing)
        app.MapPost("/api/webhooks/sandbox", ProcessSandboxWebhookAsync)
            .WithName("SandboxWebhook")
            .WithTags("Webhooks")
            .AllowAnonymous()
            .WithOpenApi(operation =>
            {
                operation.Summary = "Sandbox webhook handler";
                operation.Description = "Handle payment notifications from Sandbox gateway (testing)";
                return operation;
            });

        // Generic webhook endpoint
        app.MapPost("/api/webhooks/{gateway}", ProcessGenericWebhookAsync)
            .WithName("GenericWebhook")
            .WithTags("Webhooks")
            .AllowAnonymous()
            .WithOpenApi(operation =>
            {
                operation.Summary = "Generic webhook handler";
                operation.Description = "Handle payment notifications from any supported gateway";
                return operation;
            });
    }

    private static async Task<IResult> ProcessZarinPalWebhookAsync(
        HttpContext context,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return await ProcessWebhookAsync(
            context,
            mediator,
            PaymentGatewayType.ZarinPal,
            cancellationToken);
    }

    private static async Task<IResult> ProcessZibalWebhookAsync(
        HttpContext context,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return await ProcessWebhookAsync(
            context,
            mediator,
            PaymentGatewayType.Zibal,
            cancellationToken);
    }

    private static async Task<IResult> ProcessSandboxWebhookAsync(
        HttpContext context,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return await ProcessWebhookAsync(
            context,
            mediator,
            PaymentGatewayType.Sandbox,
            cancellationToken);
    }

    private static async Task<IResult> ProcessGenericWebhookAsync(
        string gateway,
        HttpContext context,
        IMediator mediator,
        CancellationToken cancellationToken)
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