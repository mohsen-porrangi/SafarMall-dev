using Carter;
using MediatR;
using Microsoft.AspNetCore.Components.Forms;
using PaymentGateway.API.Features.Query.GetPaymentStatus;
using System.Threading;

namespace PaymentGateway.API.Features.Endpoints;

/// <summary>
/// Endpoint دریافت وضعیت پرداخت
/// </summary>
public class GetPaymentStatusEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/payments/{paymentId}/status", async (
             string paymentId,
             IMediator mediator,
             CancellationToken cancellationToken
            ) =>
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
        })
            .WithName("GetPaymentStatus")
            .WithTags("Payments")
            .WithSummary("Get payment status")
            .WithDescription("PaymentId دریافت وضعیت فعلی پرداخت با استفاده از");

        app.MapGet("/api/payments/status", async (
             string gatewayReference,
             IMediator mediator,
             CancellationToken cancellationToken
            ) =>
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
        })
            .WithName("GetPaymentStatusByReference")
            .WithTags("Payments")
            .WithDescription("دریافت وضعیت فعلی پرداخت با gateway reference ")
            .WithSummary("Get payment status");
    }
}