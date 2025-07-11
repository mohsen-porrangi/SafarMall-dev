using Azure.Core;
using BuildingBlocks.Contracts;
using BuildingBlocks.Enums;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using PaymentGateway.API.Features.Command.CreatePayment;
using System.Security.Claims;
using System.Threading;

namespace PaymentGateway.API.Features.Endpoints;

/// <summary>
/// Endpoint ایجاد پرداخت
/// Updated to extract UserId from JWT token
/// </summary>
public class CreatePaymentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/payments", async (
            CreatePaymentCommand command,
            IMediator mediator,
            HttpContext httpContext,
            ICurrentUserService userService,
            CancellationToken cancellationToken) =>
        {           
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

            return result.ErrorCode switch
            {
                // Gateway errors
                var code when code.StartsWith("ZIBAL_") => Results.Problem(
                    statusCode: 502,
                    title: "Gateway Error",
                    detail: result.ErrorMessage
                ),

                // Validation errors  
                var code when code.StartsWith("INVALID_") => Results.BadRequest(new
                {
                    success = false,
                    error = result.ErrorMessage,
                    errorCode = result.ErrorCode
                }),

                // Default
                _ => Results.Problem(
                    statusCode: 500,
                    title: "Error",
                    detail: result.ErrorMessage ?? "خطای غیرمنتظره"
                )
            };            
        })
        .WithName("CreatePayment")
        .WithTags("Payment")
        .RequireAuthorization() 
        .WithSummary("Create new payment")
        .WithDescription("ایجاد درخواست پرداخت جدید از طریق درگاه پرداخت");
    } 
}