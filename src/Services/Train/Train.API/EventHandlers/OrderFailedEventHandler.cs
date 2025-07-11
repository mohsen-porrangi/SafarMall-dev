//using BuildingBlocks.Contracts.Services;
//using BuildingBlocks.Messaging.Events.OrderEvents;
//using BuildingBlocks.Messaging.Handlers;
//using Train.API.Services;

//namespace Train.API.EventHandlers;

///// <summary>
///// Handles order failure events and performs train reservation compensation
///// </summary>
//public class OrderFailedEventHandler(
//     RajaServices rajaServices,
//     ITrainService trainReservationService,
//     ILogger<OrderFailedEventHandler> logger
//    ) : IIntegrationEventHandler<OrderCreationFailedIntegrationEvent>
//{
//    public async Task HandleAsync(OrderCreationFailedIntegrationEvent @event, CancellationToken cancellationToken = default)
//    {
//        logger.LogWarning(
//            "Order creation failed for reservation {ReservationId}. Reason: {Reason}. Starting compensation...",
//            @event.TrainReservationId, @event.Reason);

//        try
//        {
//            // Get stored reservation data
//            var reservationData = await trainReservationService.GetReservationDataAsync(@event.TrainReservationId);

//            if (reservationData == null)
//            {
//                logger.LogWarning("No reservation data found for {ReservationId}", @event.TrainReservationId);
//                return;
//            }

//            // Use stored token or fallback to event token
//            var reserveToken = reservationData.ReserveToken ?? @event.ReserveToken;

//            if (string.IsNullOrEmpty(reserveToken))
//            {
//                logger.LogError("No reserve token available for compensation of {ReservationId}",
//                    @event.TrainReservationId);
//                return;
//            }

//            logger.LogInformation(
//                "Starting train reservation cancellation for {ReservationId} with token {Token}",
//                @event.TrainReservationId, reserveToken);

//            // Perform actual cancellation
//            var cancellationResult = await rajaServices.ReserveCancelationAsync(reserveToken);

//            if (cancellationResult.Any(c => c.StatusCode == 200)) // Assuming 200 means success
//            {
//                logger.LogInformation(
//                    "Successfully cancelled train reservation {ReservationId}",
//                    @event.TrainReservationId);
//            }
//            else
//            {
//                logger.LogWarning(
//                    "Train reservation cancellation may have failed for {ReservationId}. Results: {@Results}",
//                    @event.TrainReservationId, cancellationResult);
//            }
//        }
//        catch (Exception ex)
//        {
//            logger.LogError(ex,
//                "Failed to compensate train reservation {ReservationId}",
//                @event.TrainReservationId);

//            // TODO: Consider implementing retry mechanism or dead letter queue
//        }
//    }
//}