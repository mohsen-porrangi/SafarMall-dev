using Carter;
using MediatR;
using PaymentGateway.API.Features.Query.GetPaymentStatus;

namespace PaymentGateway.API.Features.Endpoints;

/// <summary>
/// Endpoint دریافت وضعیت پرداخت
/// </summary>
public class GetPaymentStatusEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/payments/{paymentId}/status", GetPaymentStatusAsync)
            .WithName("GetPaymentStatus")
            .WithTags("Payments")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Get payment status";
                operation.Description = "Get current status of a payment by PaymentId";
                return operation;
            });

        app.MapGet("/api/payments/status", GetPaymentStatusByReferenceAsync)
            .WithName("GetPaymentStatusByReference")
            .WithTags("Payments")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Get payment status by gateway reference";
                operation.Description = "Get current status of a payment by gateway reference";
                return operation;
            });
    }

    private static async Task<IResult> GetPaymentStatusAsync(
        string paymentId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetPaymentStatusQuery
        {
            PaymentId = paymentId
        };

        var result = await mediator.Send(query, cancellationToken);

        return Results.Ok(new
        {
            success = true,
            data = result
        });
    }

    private static async Task<IResult> GetPaymentStatusByReferenceAsync(
        string gatewayReference,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetPaymentStatusQuery
        {
            PaymentId = string.Empty, // Will be resolved by gateway reference
            GatewayReference = gatewayReference
        };

        var result = await mediator.Send(query, cancellationToken);

        return Results.Ok(new
        {
            success = true,
            data = result
        });
    }
}