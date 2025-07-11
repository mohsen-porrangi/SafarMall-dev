using BuildingBlocks.Messaging.Contracts;
using BuildingBlocks.Messaging.Events.OrderEvents;
using BuildingBlocks.Messaging.Events.TrainEvents;
using BuildingBlocks.Messaging.Handlers;
using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Features.Command.Orders.CreateOrder;


namespace Order.Application.EventHandlers;

public class TrainReservedEventHandler(
    ISender mediator,
    IMessageBus messageBus,
    ILogger<TrainReservedEventHandler> logger)
    : IIntegrationEventHandler<TrainReservedIntegrationEvent>
{
    public async Task HandleAsync(TrainReservedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing train reservation event for reservation {ReservationId}",
            @event.ReservationId);

        try
        {
            // Use proper Command pattern instead of direct creation
            var createOrderCommand = new CreateOrderCommand
            {
                UserId = @event.UserId,
                ServiceType = @event.ServiceType,
                SourceName = @event.SourceName,
                DestinationName = @event.DestinationName,
                DepartureDate = @event.DepartureDate,
                ReturnDate = @event.ReturnDate,
                TrainNumber = @event.TrainNumber.ToString(),
                ProviderId = @event.ProviderId,
                BasePrice = @event.FullPrice,
                Passengers = @event.Passengers.Select(p => new CreateOrderPassengerInfo(
                    p.FirstNameEn,
                    p.LastNameEn,
                    p.FirstNameFa, 
                    p.LastNameFa,  
                    p.BirthDate,
                    p.Gender,
                    p.IsIranian,
                    p.NationalCode,
                    p.PassportNo
                )).ToList()
            };

            // Create order through proper CQRS pattern
            var result = await mediator.Send(createOrderCommand, cancellationToken);

            // Publish success event
            var successEvent = new OrderCreatedFromTrainIntegrationEvent(
                result.OrderId,
                @event.UserId,
                result.OrderNumber,
                @event.ServiceType,
                result.TotalAmount,
                @event.ReservationId,
                @event.TrainNumber
            );

            await messageBus.PublishAsync(successEvent, cancellationToken);

            logger.LogInformation("Successfully created order {OrderId} from train reservation {ReservationId}",
                result.OrderId, @event.ReservationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create order from train reservation {ReservationId}",
             @event.ReservationId);

            // 🔥 Updated: Include more context for compensation
            var failureEvent = new OrderCreationFailedIntegrationEvent(
                trainReservationId: @event.ReservationId,
                userId: @event.UserId,
                reason: "Order creation failed",
                errorDetails: ex.Message,
                reserveToken: null // Will be retrieved from storage
            );

            await messageBus.PublishAsync(failureEvent, cancellationToken);
        }
    }
}