using BuildingBlocks.Contracts;
using BuildingBlocks.Contracts.Services;
using BuildingBlocks.Enums;
using BuildingBlocks.Exceptions;
using BuildingBlocks.Models.DTOs;
using Train.API.Models.Requests;
using Train.API.Models.Responses;
using Train.API.Services;

namespace Train.API.Services;

/// <summary>
/// Enhanced train reservation service with Order Service integration
/// Implements new approach: Reserve → Create Order → Cache Complete Order
/// </summary>
public class TrainReservationService(
    RajaServices rajaServices,
    IOrderExternalService orderService,
    IRedisCacheService cacheService,
    ILogger<TrainReservationService> logger
    ) : ITrainService
{
    /// <summary>
    /// Reserve train for passengers with integrated order creation
    /// New Method Name: ReserveTrainWithOrderAsync (was ReserveTrainForPassengerWithEventAsync)
    /// </summary>
    public async Task<TrainReservationResult> ReserveTrainForPassengerWithOrderAsync(
        TrainPassengerReserveRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await ReserveTrainWithOrderAsync(request, userId, cancellationToken);
    }

    /// <summary>
    /// Reserve train for car transport with integrated order creation
    /// New Method Name: ReserveCarTransportWithOrderAsync (was ReserveTrainForCarWithEventAsync)
    /// </summary>
    public async Task<TrainReservationResult> ReserveTrainForCarWithOrderAsync(
        TrainCarReserveRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await ReserveCarTransportWithOrderAsync(request, userId, cancellationToken);
    }

    /// <summary>
    /// Get stored reservation data for compensation scenarios
    /// </summary>
    public async Task<TrainReservationData?> GetReservationDataAsync(Guid reservationId)
    {
        return await cacheService.GetAsync<TrainReservationData>(
            reservationId.ToString(),
            BusinessPrefixKeyEnum.TrainReserveConfirmation);
    }

    #region Private Implementation Methods

    /// <summary>
    /// Core implementation: Reserve train with integrated order creation
    /// </summary>
    private async Task<TrainReservationResult> ReserveTrainWithOrderAsync(
        TrainPassengerReserveRequest request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var reservationId = Guid.NewGuid();

        logger.LogInformation("Starting train reservation with order creation for user {UserId}, ReservationId {ReservationId}",
            userId, reservationId);

        try
        {
            // Step 1: Reserve train through Raja API
            var internalRequest = MapToInternalRequest(request);
            // var reservationResult = await rajaServices.ReserveTrainAsync(internalRequest);
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
            TotalPrice = 4350000,
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

            logger.LogInformation("Train reservation completed: ReservationId {ReservationId}, TrainCount {TrainCount}",
                reservationId, reservationResult.Trains.Count);

            // Step 2: Create order in Order Service
            var orderRequest = MapReservationToOrderRequest(reservationResult, userId);
            var orderResult = await orderService.CreateTrainOrderAsync(orderRequest, cancellationToken);

            if (!orderResult.Success)
            {
                logger.LogWarning("Order creation failed, cancelling train reservation: {ErrorMessage}", orderResult.ErrorMessage);

                // Compensation: Cancel train reservation
                await CancelTrainReservationAsync(reservationResult.ReserveConfirmationToken);

                return new TrainReservationResult
                {
                    IsSuccess = false,
                    ErrorMessage = orderResult.ErrorMessage ?? "خطا در ایجاد سفارش",
                    ReservationId = reservationId.ToString()
                };
            }

            // Step 3: Cache complete order data in Redis
            await CacheOrderDataAsync(reservationId, orderResult, reservationResult.ReserveConfirmationToken);

            logger.LogInformation("Train reservation and order creation completed successfully: ReservationId {ReservationId}, OrderId {OrderId}",
                reservationId, orderResult.OrderId);

            return new TrainReservationResult
            {
                IsSuccess = true,
                ReservationId = reservationId.ToString(),
                OrderId = orderResult.OrderId,
                OrderNumber = orderResult.OrderNumber
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during train reservation: ReservationId {ReservationId}", reservationId);

            return new TrainReservationResult
            {
                IsSuccess = false,
                ErrorMessage = "خطای سیستمی در فرایند رزرو",
                ReservationId = reservationId.ToString()
            };
        }
    }

    /// <summary>
    /// Core implementation: Reserve car transport with integrated order creation
    /// </summary>
    private async Task<TrainReservationResult> ReserveCarTransportWithOrderAsync(
        TrainCarReserveRequest request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var reservationId = Guid.NewGuid();

        logger.LogInformation("Starting car transport reservation with order creation for user {UserId}, ReservationId {ReservationId}",
            userId, reservationId);

        // Validation: Car transport should have exactly 1 passenger
        if (request.CarOwner.Count != 1)
        {
            return new TrainReservationResult
            {
                IsSuccess = false,
                ErrorMessage = "تعداد مسافر برای حمل خودرو باید دقیقاً یک نفر باشد",
                ReservationId = reservationId.ToString()
            };
        }

        try
        {
            // Step 1: Reserve car transport through Raja API
            var internalRequest = MapCarRequestToInternalRequest(request);
            var reservationResult = await rajaServices.ReserveTrainAsync(internalRequest);

            logger.LogInformation("Car transport reservation completed: ReservationId {ReservationId}", reservationId);

            // Step 2: Create order in Order Service (similar to passenger reservation)
            var orderRequest = MapCarReservationToOrderRequest(reservationResult, userId, request.CarInformation);
            var orderResult = await orderService.CreateTrainOrderAsync(orderRequest, cancellationToken);

            if (!orderResult.Success)
            {
                logger.LogWarning("Car transport order creation failed, cancelling reservation: {ErrorMessage}", orderResult.ErrorMessage);

                // Compensation: Cancel reservation
                await CancelTrainReservationAsync(reservationResult.ReserveConfirmationToken);

                return new TrainReservationResult
                {
                    IsSuccess = false,
                    ErrorMessage = orderResult.ErrorMessage ?? "خطا در ایجاد سفارش حمل خودرو",
                    ReservationId = reservationId.ToString()
                };
            }

            // Step 3: Cache complete order data
            await CacheOrderDataAsync(reservationId, orderResult, reservationResult.ReserveConfirmationToken);

            logger.LogInformation("Car transport reservation and order creation completed: ReservationId {ReservationId}, OrderId {OrderId}",
                reservationId, orderResult.OrderId);

            return new TrainReservationResult
            {
                IsSuccess = true,
                ReservationId = reservationId.ToString(),
                OrderId = orderResult.OrderId,
                OrderNumber = orderResult.OrderNumber
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during car transport reservation: ReservationId {ReservationId}", reservationId);

            return new TrainReservationResult
            {
                IsSuccess = false,
                ErrorMessage = "خطای سیستمی در فرایند رزرو حمل خودرو",
                ReservationId = reservationId.ToString()
            };
        }
    }

    #endregion

    #region Mapping Methods

    /// <summary>
    /// Map external passenger request to internal Raja request format
    /// </summary>
    private TrainReserveRequestDto MapToInternalRequest(TrainPassengerReserveRequest request)
    {
        return new TrainReserveRequestDto
        {
            MainPassengerTel = request.MainPassengerTel,
            CaptchaId = request.CaptchaId,
            CaptchVal = request.CaptchVal,
            ReserveToken = request.ReserveToken,
            IsExclusiveDepart = request.IsExclusiveDepart,
            IsExclusiveReturn = request.IsExclusiveReturn,
            Passengers = request.Passengers.Select(MapPassengerToInternal).ToList()
        };
    }

    /// <summary>
    /// Map external car request to internal Raja request format
    /// </summary>
    private TrainReserveRequestDto MapCarRequestToInternalRequest(TrainCarReserveRequest request)
    {
        return new TrainReserveRequestDto
        {
            MainPassengerTel = request.MainPassengerTel,
            CaptchaId = request.CaptchaId,
            CaptchVal = request.CaptchVal,
            ReserveToken = request.ReserveToken,
            IsExclusiveDepart = request.IsExclusiveDepart,
            IsExclusiveReturn = request.IsExclusiveReturn,
            Passengers = request.CarOwner.Select(MapPassengerToInternal).ToList()
        };
    }

    /// <summary>
    /// Map passenger request to internal format
    /// </summary>
    private PassengerReserveRequestDTO MapPassengerToInternal(TrainPassengerRequest passenger)
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
    /// Map train reservation result to order service request
    /// </summary>
    private CreateTrainOrderRequest MapReservationToOrderRequest(ReserveResponseDTO reservation, Guid userId)
    {
        var firstTrain = reservation.Trains.First();

        return new CreateTrainOrderRequest
        {
            ServiceType = ServiceType.Train,
            SourceName = firstTrain.SourceName,
            DestinationName = firstTrain.DestinationName,
            DepartureDate = firstTrain.DepartureDate,
            ReturnDate = firstTrain.ReturnDate,
            TrainNumber = firstTrain.TrainNumber.ToString(),
            ProviderId = firstTrain.ProviderId,
            BasePrice = firstTrain.TotalPrice,
            SeatCount = firstTrain.SeatCount,
            TicketType = firstTrain.TicketType,
            Passengers = firstTrain.Passengers.Select(MapToTrainPassengerInfo).ToList()
        };
    }

    /// <summary>
    /// Map car reservation result to order service request
    /// </summary>
    private CreateTrainOrderRequest MapCarReservationToOrderRequest(
        ReserveResponseDTO reservation,
        Guid userId,
        CarInfoRequest carInfo)
    {
        var orderRequest = MapReservationToOrderRequest(reservation, userId);

        // Add car-specific information to order description
        // This could be extended with proper car transport fields in Order Service

        return orderRequest;
    }

    /// <summary>
    /// Map reservation passenger to TrainPassengerInfo
    /// Uses TrainReservedDTO.Passengers from IOrderExternalService
    /// </summary>
    private TrainPassengerInfo MapToTrainPassengerInfo(OrderPassengerInfo passenger)
    {
        return new TrainPassengerInfo
        {
            FirstNameFa = passenger.IsIranian ? passenger.FirstName : string.Empty,
            LastNameFa = passenger.IsIranian ? passenger.LastName : string.Empty,
            FirstNameEn = !passenger.IsIranian ? passenger.FirstName : string.Empty,
            LastNameEn = !passenger.IsIranian ? passenger.LastName : string.Empty,
            BirthDate = passenger.BirthDate,
            Gender = passenger.Gender,
            IsIranian = passenger.IsIranian,
            NationalCode = passenger.NationalCode,
            PassportNo = passenger.PassportNo
        };
    }

    #endregion

    #region Cache and Compensation Methods

    /// <summary>
    /// Cache complete order data in Redis for payment processing
    /// </summary>
    private async Task CacheOrderDataAsync(
        Guid reservationId,
        CreateTrainOrderResponse orderResult,
        string confirmationToken)
    {
        var cacheData = new TrainOrderCacheData
        {
            ReservationId = reservationId,
            OrderId = orderResult.OrderId!.Value,
            OrderNumber = orderResult.OrderNumber!,
            TotalAmount = orderResult.TotalAmount,
            Status = orderResult.Status,
            ServiceType = orderResult.ServiceType,
            SourceName = orderResult.SourceName,
            DestinationName = orderResult.DestinationName,
            DepartureDate = orderResult.DepartureDate,
            ReturnDate = orderResult.ReturnDate,
            PassengerCount = orderResult.PassengerCount,
            CreatedAt = orderResult.CreatedAt,
            Trains = orderResult.Trains,
            Passengers = orderResult.Passengers,
            ConfirmationToken = confirmationToken
        };

        // Cache for 2 hours - enough time for payment processing
        await cacheService.SetAsync(
            reservationId.ToString(),
            cacheData,
            TimeSpan.FromHours(2),
            BusinessPrefixKeyEnum.TrainOrder);

        logger.LogInformation("Order data cached successfully: ReservationId {ReservationId}, OrderId {OrderId}",
            reservationId, orderResult.OrderId);
    }

    /// <summary>
    /// Cancel train reservation in case of order creation failure
    /// Uses existing RajaServices.ReserveCancelationAsync method
    /// </summary>
    private async Task CancelTrainReservationAsync(string confirmationToken)
    {
        try
        {
            logger.LogInformation("Cancelling train reservation with token: {Token}", confirmationToken);

            var cancellationResult = await rajaServices.ReserveCancelationAsync(confirmationToken);

            if (cancellationResult.Any(c => c.StatusCode == 200))
            {
                logger.LogInformation("Train reservation cancelled successfully: {Token}", confirmationToken);
            }
            else
            {
                logger.LogWarning("Train reservation cancellation may have failed: {Token}, Results: {@Results}",
                    confirmationToken, cancellationResult);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to cancel train reservation: {Token}", confirmationToken);
            // Don't throw - this is compensation, log and continue
        }
    }

    #endregion
}

/// <summary>
/// Complete train order cache data structure
/// Contains all necessary information for payment processing and order management
/// </summary>
public record TrainOrderCacheData
{
    public Guid ReservationId { get; init; }
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public ServiceType ServiceType { get; init; }
    public string SourceName { get; init; } = string.Empty;
    public string DestinationName { get; init; } = string.Empty;
    public DateTime DepartureDate { get; init; }
    public DateTime? ReturnDate { get; init; }
    public int PassengerCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<OrderTrainInfo>? Trains { get; init; }
    public List<TrainPassengerInfo>? Passengers { get; init; }
    public string ConfirmationToken { get; init; } = string.Empty; // For Raja cancellation
}