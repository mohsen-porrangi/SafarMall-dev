using BuildingBlocks.Contracts;
using BuildingBlocks.Enums;
using Carter;
using MediatR;
using Order.Application.Features.Command.Orders.CreateOrder;

namespace Order.API.Endpoints.Internal.Orders;

public class CreateOrderInternalEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/internal/train-orders", async (
            CreateTrainOrderInternalRequest request,
            ICurrentUserService userService,
            ISender sender,
            CancellationToken ct) =>
        {
            var command = new CreateOrderCommand
            {
                UserId = userService.GetCurrentUserId(),
                ServiceType = request.ServiceType,
                SourceCode = request.SourceCode,
                DestinationCode = request.DestinationCode,
                BasePrice = request.TotalPrice,
                SourceName = request.SourceName,
                DestinationName = request.DestinationName,
                DepartureDate = request.DepartureDate,
                ReturnDate = request.ReturnDate,
                Passengers = request.Passengers,
                TrainNumber = request.TrainNumber,
                ProviderId = request.ProviderId
            };

            var result = await sender.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("CreateTrainOrderInternal")
        .WithDescription("ایجاد سفارش توسط سرویس‌های داخلی")
        .Produces<CreateOrderResult>(StatusCodes.Status200OK)
        .WithTags("Internal")
        .AllowAnonymous();
    }
}

public record CreateTrainOrderInternalRequest(
    ServiceType ServiceType,
    int? SourceCode,
    int? DestinationCode,
    string SourceName,
    string DestinationName,
    int seatCount,
    int TicketType,
    decimal TotalPrice,
    DateTime DepartureDate,
    DateTime? ReturnDate,
    List<CreateOrderPassengerInfo> Passengers,
    string? FlightNumber,
    string? TrainNumber,
    int ProviderId);