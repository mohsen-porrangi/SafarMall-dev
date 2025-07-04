using BuildingBlocks.Enums;
using Carter;
using MediatR;
using PaymentGateway.API.Features.Command.CreatePayment;
using System.Security.Claims;

namespace PaymentGateway.API.Features.Endpoints;

/// <summary>
/// Endpoint ایجاد پرداخت
/// Updated to extract UserId from JWT token
/// </summary>
public class CreatePaymentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/payments", CreatePaymentAsync)
            .WithName("CreatePayment")
            .WithTags("Payment")
            .RequireAuthorization() // اضافه شده - نیاز به authentication
            .WithOpenApi(operation =>
            {
                operation.Summary = "Create new payment";
                operation.Description = "Create a new payment request through payment gateway";
                return operation;
            });
    }

    /// <summary>
    /// مدل درخواست ایجاد پرداخت
    /// </summary>
    public record CreatePaymentRequest(
        decimal Amount,
        string Description,
        string CallbackUrl,
        int GatewayType = 2, // Default: Zibal
        string? OrderId = null
    );

    private static async Task<IResult> CreatePaymentAsync(
        CreatePaymentRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // استخراج UserId از JWT token
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Unauthorized();
        }

        var command = new CreatePaymentCommand
        {
            UserId = userId, // اضافه شده
            Amount = request.Amount,
            Description = request.Description,
            CallbackUrl = request.CallbackUrl,
            GatewayType = (PaymentGatewayType)request.GatewayType,
            OrderId = request.OrderId
        };

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsSuccessful)
        {
            return Results.Ok(new
            {
                success = true,
                paymentId = result.PaymentId,
                paymentUrl = result.PaymentUrl,
                gatewayReference = result.GatewayReference,
                expiresAt = result.ExpiresAt
            });
        }

        return Results.BadRequest(new
        {
            success = false,
            error = result.ErrorMessage,
            errorCode = result.ErrorCode
        });
    }
}