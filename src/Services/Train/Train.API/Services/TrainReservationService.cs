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
            var reservationResult = await rajaServices.ReserveTrainAsync(internalRequest);

    //        var reservationResult = new ReserveResponseDTO
    //        {
    //            ReserveConfirmationToken = "8QVMaB9y6pIepGRUaPVpecDAeUJpX-nr1XNOdMAC-YfGfz56f_Op0RVC3-_cu3wAvjM2bucaW1zZJZa-hK0MDWbl6TKbWFbmeGqrYWXKcpDfNasrwd6Q4QGY4NBbExDDdqP2Uiw-mziIuv4WnDNTv5cx-Euux_2_ZZk6ZtyBvnrn5Sk3dV1JRo5d7PO_1Fhs57xbj4AkK1922k4gKAc4k6EQSKCzquYkigoI6O20FfgUb4HAk0tyIHlzvX_-Qofjte_INa5b2oJs3DDsMnpktEn6r4jRY7L_eELL2K-zkrV3OEr-LAk6LG-DTXPwEjRUmVMdwu41cP6OrqTJxRl7H7WtYMOHPBCnP1_T0SMdmQS7mtaV8T_nwV3RlNYbDnN6T1n_ew_s08i38Cn1Dn6laqash2x0FA5KFegCJCAHjhvqghXoAEj96L5ck6zc44DCywxVi2BFqOK9Iu7OR5d6iUW25Ab1j1b873o1ydw_GEy9lZ5EOa4j9vAh8i4ALcfpX_rDfqOTvRJS_nUZ2mwWvjAspQk58QG1cTx2NI2M-tzhdtHDEQtidr2CC8Rnuk5UktmpNReFseAu9lz9ilgsNg1i41NqkWfGJNBLUK0Qsr7I0RC-hYn8v_rWzROLdD15",

    //            Trains = new List<TrainReservedResponseDTO>
    //{
    //    new TrainReservedResponseDTO
    //    {
    //        MainPassengerTel = "09125485347",
    //        IsExclusive = false,
    //        ExclusiveAmount = 0,
    //        SeatCount = 1,
    //        FullPrice = 8550000,
    //        TrainNumber = 372,
    //        ReserveId = 780464137,
    //        TicketType = 1,
    //        ProviderId = 0,
    //        SourceName = "تهران",
    //        DestinationName = "مشهد",
    //        DepartureDate = DateTime.MinValue,
    //        ReturnDate = DateTime.MinValue,
    //        DepartureDatePersian = "1405/04/27",
    //        DepartureTime = "19:20",
    //        WagonNumbers = "2",
    //        CompartmentNumbers = "17",
    //        RequestDateTime = new DateTime(2025, 6, 28, 12, 4, 50, 18, DateTimeKind.Local).AddTicks(729),
    //        Passengers = new List<OrderPassengerInfo>
    //        {
    //            new OrderPassengerInfo(
    //                FirstName: "محسن",
    //                LastName: "پررنگی",
    //                BirthDate: new DateTime(1984, 3, 21),
    //                Gender: Gender.Male, // چون مقدار JSON عددی بود: 0 = Male
    //                IsIranian: true,
    //                NationalCode: "0075436851",
    //                PassportNo: ""
    //            )
    //        }
    //    }
    //}
    //        };


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
            FirstName = passenger.FirstName,
            LastName = passenger.LastName,
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
