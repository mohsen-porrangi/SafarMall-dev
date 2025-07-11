//using BuildingBlocks.Enums;
//using Carter;
//using MediatR;
//using Order.Application.Features.Command.ProcessPayment;


//namespace Order.API.Endpoints.Internal.Payments;

///// <summary>
///// Internal payment processing endpoint for service-to-service communication
///// </summary>
//public class ProcessOrderPaymentInternalEndpoint : ICarterModule
//{
//    public void AddRoutes(IEndpointRouteBuilder app)
//    {
//        app.MapPost("/api/internal/orders/{orderId:guid}/payment/process", async (
//            Guid orderId,
//            ProcessOrderPaymentInternalRequest request,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var command = new ProcessOrderPaymentCommand(
//                orderId,
//                request.PaymentGateway,
//                request.Description
//                );

//            var result = await sender.Send(command, ct);

//            return Results.Ok(new
//            {
//                success = result.IsSuccessful,
//                orderId = result.OrderId,
//                paymentType = result.PaymentType.ToString(),
//                totalAmount = result.TotalAmount,
//                paymentUrl = result.PaymentUrl,
//                authority = result.Authority,
//                message = result.IsSuccessful ? "پرداخت با موفقیت پردازش شد" : result.ErrorMessage
//            });
//        })
//        .WithName("ProcessOrderPaymentInternal")
//        .WithDescription("پردازش پرداخت سفارش توسط سرویس‌های داخلی")
//        .Produces<object>(StatusCodes.Status200OK)
//        .Produces(StatusCodes.Status400BadRequest)
//        .Produces(StatusCodes.Status404NotFound)
//        .WithTags("Internal-Payments")
//        .AllowAnonymous();
//    }
//}

///// <summary>
///// Internal payment processing request
///// </summary>
//public record ProcessOrderPaymentInternalRequest(
//    PaymentGatewayType PaymentGateway = PaymentGatewayType.Zibal,
//    string Description = ""
//);