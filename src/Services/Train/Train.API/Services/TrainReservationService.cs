using BuildingBlocks.Contracts;
using BuildingBlocks.Contracts.Services;
using BuildingBlocks.Enums;
using BuildingBlocks.Messaging.Contracts;
using BuildingBlocks.Messaging.Events.TrainEvents;
using BuildingBlocks.Models.DTOs;
using Train.API.Models.Requests;
using Train.API.Models.Responses;
using static BuildingBlocks.Contracts.Services.IOrderExternalService;

namespace Train.API.Services;

public class TrainReservationService(
    RajaServices rajaServices,
    IMessageBus messageBus,
    IRedisCacheService cacheService,
    ILogger<TrainReservationService> logger
    ) : ITrainService
{
    public async Task<TrainReservationResult> ReserveTrainWithEventAsync(
        TrainReserveRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting train reservation for user {UserId}", userId);

        try
        {
            var internalRequest = MapToInternalRequest(request);
            //   var reservationResult = await rajaServices.ReserveTrainAsync(internalRequest);

            var reservationResult = new ReserveResponseDTO
            {
                ReserveConfirmationToken = "KeNg2iQ18v6pHnV554ops9RN7F_XDT1OmMW6d0YzuR4Ix_-nJrZHgYLDk0IjjXZELTE3Qq8W5WFoHKZjQsYq5PH45p58KNKKG2XafKaOR3j872roVSHfVXc_X1tqW3twg34LQFviFV4gTJqN3g_ENeu0eOTEvj18F2qty5ZJgdGDlh3RhlZ_oz8rs3gvFeyy64eRgmNuXBV0YS1DRTsFjrR2_3Y0yZHZWrLxWLglwBJ1Z67jysteHAjE_OUFO-w-mm98JOeWc0SWbNIyuqqaawd9Zj91cldG1x67bvf3ND0RDsQ8PSvM5PF1KrorZTm1jaKRAP0vtt2mH6xRoSMUptXpM9mvMLJrMWn-xgcLsRHIIRuQQNLefY-gaVeLWTtkCCThlcJd-G9R3xLVluEI_HFS3_i9uzYuthbOROmhPety_yyH8XFF7RFbJhLGLeJytWzlmKS0Og6vw0p7uN3g_U9ul_ep0C1OVxKfKMB1mJFVbOz3HbaFRfF98MQCNVCR54xOeYSRYMZF1S06l8rfJN92dql32Fq11WU3pfyTn3ShvpJTXgKC2eUmohK9uymEA0xuAEIbTDhkzWwsZ_uoy-JHZb8wfxNHHCx2_RRDUovN4xDxe0SD0lS7T57OUUd5",
                Trains = new List<TrainReservedDTO>
    {
        new TrainReservedDTO
        {
            MainPassengerTel = "09125485347",
            IsExclusive = false,
            ExclusiveAmount = 0,
            SeatCount = 1,
            FullPrice = 3350000,
            TrainNumber = 318,
            ReserveId = 780992281,
            TicketType = 1,
            ProviderId = 1, // ownerCode in JSON
            SourceName = "تهران",
            DestinationName = "مشهد",
            DepartureDate = new DateTime(2025, 7, 16),
            DepartureDatePersian = "1404/04/25",
            DepartureTime = "07:50:00",
            WagonNumbers = "2",
            CompartmentNumbers = "27",
            ReturnDate = null, // چون در JSON نیست
            RequestDateTime = DateTime.Parse("2025-06-30T13:55:32.2810359+03:30"),
            Passengers = new List<OrderPassengerInfo>
            {
                new OrderPassengerInfo(
                    FirstName: "محسن",
                    LastName: "پررنگی",
                    BirthDate: new DateTime(1984, 3, 21),
                    Gender: Gender.Male, // PassengerType = 1 → فرض بر Male بودن
                    IsIranian: true,
                    NationalCode: "0075436851",
                    PassportNo: ""
                )
            }
        }
    }
            };


            var reservationIds = await PublishTrainReservedEventsAsync(
                reservationResult, userId, request.ReserveToken, cancellationToken);

            return new TrainReservationResult
            {
                IsSuccess = true,
                CreatedReservationIds = reservationIds
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Train reservation failed for user {UserId}", userId);

            return new TrainReservationResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<TrainReservationData?> GetReservationDataAsync(Guid reservationId)
    {
        return await cacheService.GetAsync<TrainReservationData>(
            $"train_reservation_{reservationId}",
            BusinessPrefixKeyEnum.Order);
    }

    private async Task<List<Guid>> PublishTrainReservedEventsAsync(
        ReserveResponseDTO reservationResult,
        Guid userId,
        string reserveToken,
        CancellationToken cancellationToken)
    {
        var reservationIds = new List<Guid>();

        foreach (var train in reservationResult.Trains)
        {
            var reservationId = Guid.NewGuid();
            reservationIds.Add(reservationId);

            // Store reservation data for compensation
            await StoreReservationDataAsync(reservationId, reserveToken, train);

            var integrationEvent = new TrainReservedIntegrationEvent(
                reservationId: reservationId,
                userId: userId,
                mainPassengerTel: train.MainPassengerTel,
                trainNumber: train.TrainNumber,
                providerId: train.ProviderId,
                sourceName: train.SourceName,
                destinationName: train.DestinationName,
                departureDate: train.DepartureDate,
                returnDate: train.ReturnDate,
                departureDatePersian: train.DepartureDatePersian,
                departureTime: train.DepartureTime,
                fullPrice: train.FullPrice,
                isExclusive: train.IsExclusive,
                exclusiveAmount: train.ExclusiveAmount,
                seatCount: train.SeatCount,
                ticketType: train.TicketType,
                wagonNumbers: train.WagonNumbers,
                compartmentNumbers: train.CompartmentNumbers,
                requestDateTime: train.RequestDateTime,
                passengers: train.Passengers.Select(MapToEventPassenger).ToList()
            );

            await messageBus.PublishAsync(integrationEvent, cancellationToken);

            logger.LogInformation("Published TrainReservedIntegrationEvent for reservation {ReservationId}",
                reservationId);
        }

        return reservationIds;
    }

    /// <summary>
    /// Maps OrderPassengerInfo (from Train API response) to TrainPassengerInfo (for event)
    /// </summary>
    private TrainPassengerInfo MapToEventPassenger(OrderPassengerInfo passenger)
    {
        return new TrainPassengerInfo
        {
            FirstNameFa = passenger.IsIranian ? passenger.FirstName : null,
            LastNameFa = passenger.IsIranian ? passenger.LastName : null,
            FirstNameEn = passenger.IsIranian ? null : passenger.FirstName,
            LastNameEn = passenger.IsIranian ? null : passenger.LastName,
            BirthDate = passenger.BirthDate,
            Gender = passenger.Gender,
            IsIranian = passenger.IsIranian,
            NationalCode = passenger.NationalCode,
            PassportNo = passenger.PassportNo
        };
    }

    /// <summary>
    /// Maps TrainReserveRequest (from BuildingBlocks) to TrainReserveRequestDto (internal Train API)
    /// </summary>
    private TrainReserveRequestDto MapToInternalRequest(TrainReserveRequest request)
    {
        return new TrainReserveRequestDto
        {
            MainPassengerTel = request.MainPassengerTel,
            CaptchaId = request.CaptchaId,
            CaptchVal = request.CaptchVal,
            ReserveToken = request.ReserveToken,
            IsExclusiveDepart = request.IsExclusiveDepart,
            IsExclusiveReturn = request.IsExclusiveReturn,
            Passengers = request.Passengers.Select(MapToInternalPassenger).ToList()
        };
    }

    /// <summary>
    /// Maps TrainPassengerRequest (from BuildingBlocks) to PassengerReserveRequestDTO (internal Train API)
    /// </summary>
    private PassengerReserveRequestDTO MapToInternalPassenger(TrainPassengerRequest passenger)
    {
        return new PassengerReserveRequestDTO
        {
            Name = passenger.Name,
            Family = passenger.Family,
            BirthDatePersian = passenger.BirthDatePersian,
            Nationalcode = passenger.Nationalcode,
            PassportNo = passenger.PassportNo,
            DepartOptionalServiceCode = passenger.DepartOptionalServiceCode,
            RetrunOptionalServiceCode = passenger.RetrunOptionalServiceCode,
            DepartFreeServiceCode = passenger.DepartFreeServiceCode,
            ReturnFreeServiceCode = passenger.ReturnFreeServiceCode,
            IsIranian = passenger.IsIranian
        };
    }

    /// <summary>
    /// Store reservation data for potential compensation
    /// </summary>
    private async Task StoreReservationDataAsync(Guid reservationId, string reserveToken, TrainReservedDTO train)
    {
        var reservationData = new TrainReservationData
        {
            ReservationId = reservationId,
            ReserveToken = reserveToken,
            TrainNumber = train.TrainNumber,
            ReserveId = train.ReserveId,
            CreatedAt = DateTime.UtcNow
        };

        // Store for 24 hours (enough time for order processing)
        await cacheService.SetAsync(
            $"train_reservation_{reservationId}",
            reservationData,
            TimeSpan.FromHours(24),
            BusinessPrefixKeyEnum.Order);
    }

    /// <summary>
    /// Get reservation data for compensation
    /// </summary>

}

/// <summary>
/// Data structure for storing train reservation info
/// </summary>
