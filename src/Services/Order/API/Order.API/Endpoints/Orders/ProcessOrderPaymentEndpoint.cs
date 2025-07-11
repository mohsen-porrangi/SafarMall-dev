using BuildingBlocks.Enums;
using Carter;
using MediatR;
using Order.Application.Features.Command.ProcessPayment;

namespace Order.API.Endpoints.Orders;

/// <summary>
/// Process order payment endpoint - Called from UI
/// Public endpoint for users to initiate payment for their orders
/// </summary>
public class ProcessOrderPaymentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/{id:guid}/payment/process", async (
            Guid id,
            ProcessOrderPaymentRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var command = new ProcessOrderPaymentCommand
            {
                OrderId = id,
                PaymentGateway = request.PaymentGateway,
                UseCredit = request.UseCredit
            };

            var result = await sender.Send(command, ct);

            // CRITICAL: Check failure FIRST
            if (!result.IsSuccessful)
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage,
                    orderId = result.OrderId,
                    orderNumber = result.OrderNumber,
                    totalAmount = result.TotalAmount
                });
            }

            // Only process success cases if IsSuccessful = true
            return result.PaymentType switch
            {
                PurchaseType.FullWallet or PurchaseType.Credit => Results.Ok(new
                {
                    success = true,
                    message = "پرداخت با موفقیت انجام شد",
                    orderId = result.OrderId,
                    orderNumber = result.OrderNumber,
                    paymentType = result.PaymentType.ToString(),
                    totalAmount = result.TotalAmount,
                    walletBalance = result.WalletBalance,
                    processedAt = result.ProcessedAt,
                    requiresRedirect = false
                }),

                PurchaseType.FullPayment or PurchaseType.Mixed => Results.Ok(new
                {
                    success = true,
                    message = "انتقال به درگاه پرداخت",
                    orderId = result.OrderId,
                    orderNumber = result.OrderNumber,
                    paymentType = result.PaymentType.ToString(),
                    totalAmount = result.TotalAmount,
                    requiredPayment = result.RequiredPayment,
                    walletBalance = result.WalletBalance,
                    paymentUrl = result.PaymentUrl,
                    authority = result.Authority,
                    requiresRedirect = true
                }),

                _ => Results.Problem(
                    title: "Unknown Payment Type",
                    detail: $"Unknown payment type: {result.PaymentType}",
                    statusCode: 500)
            };
        })
        .WithName("ProcessOrderPayment")
        .WithSummary("پردازش پرداخت سفارش")
        .WithDescription("پردازش پرداخت سفارش از طریق کیف پول و/یا درگاه پرداخت")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces<object>(StatusCodes.Status400BadRequest)
        .Produces<Microsoft.AspNetCore.Mvc.ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<Microsoft.AspNetCore.Mvc.ProblemDetails>(StatusCodes.Status403Forbidden)
        .Produces<Microsoft.AspNetCore.Mvc.ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<Microsoft.AspNetCore.Mvc.ProblemDetails>(StatusCodes.Status500InternalServerError)
        .WithTags("Orders")
        .RequireAuthorization();
    }
}

/// <summary>
/// Request model for processing order payment
/// </summary>
public record ProcessOrderPaymentRequest
{
    /// <summary>
    /// Payment gateway type (ZarinPal, Zibal, etc.)
    /// </summary>
    public PaymentGatewayType PaymentGateway { get; init; } = PaymentGatewayType.ZarinPal;

    /// <summary>
    /// Whether to use credit payment (B2B feature)
    /// </summary>
    public bool UseCredit { get; init; } = false;
}