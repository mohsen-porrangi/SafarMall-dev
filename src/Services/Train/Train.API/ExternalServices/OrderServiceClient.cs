using BuildingBlocks.Contracts.Services;
using Train.API.Common;
using static BuildingBlocks.Contracts.Services.IOrderExternalService;

namespace Train.API.ExternalServices
{
    public class OrderServiceClient(
        HttpClient httpClient,
        ILogger<OrderServiceClient> logger)
        : BaseHttpClient(httpClient, logger), IOrderExternalService

    {

        public async Task<bool> CreateTrainOrderAsync(CreateOrderInternalRequest request, CancellationToken cancellationToken)
        {

            var internalRequest = new CreateOrderInternalRequest()
            {
                Trains = request.Trains.Select(train => new TrainReservedDTO
                {
                    MainPassengerTel = train.MainPassengerTel,
                    IsExclusive = train.IsExclusive,
                    ExclusiveAmount = train.ExclusiveAmount,
                    SeatCount = train.SeatCount,
                    FullPrice = train.FullPrice,
                    TrainNumber = train.TrainNumber,
                    ReserveId = train.ReserveId,
                    TicketType = train.TicketType,
                    ProviderId = train.ProviderId,
                    SourceName = train.SourceName,
                    DestinationName = train.DestinationName,
                    DepartureDate = train.DepartureDate,
                    ReturnDate = train.ReturnDate,
                    DepartureDatePersian = train.DepartureDatePersian,
                    DepartureTime = train.DepartureTime,
                    WagonNumbers = train.WagonNumbers,
                    CompartmentNumbers = train.CompartmentNumbers,
                    RequestDateTime = train.RequestDateTime,
                    Passengers = train.Passengers
                }).ToList(),
                ReserveConfirmationToken = request.ReserveConfirmationToken
            };


            var allPassengers = request.Trains
            .SelectMany(train => train.Passengers.Select(p => new OrderPassengerInfo(
                p.FirstName,
                p.LastName,
                p.BirthDate,
                p.Gender,
                p.IsIranian,
                p.NationalCode,
                p.PassportNo
            )))
            .ToList();
            var response = await PostAsync<CreateOrderInternalRequest, CreateOrderResponse>(
           "/api/internal/order", internalRequest, cancellationToken);

            return response?.Success ?? false;
        }

    }
}
