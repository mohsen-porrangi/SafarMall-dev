namespace BuildingBlocks.Contracts.Services;

public interface ITrainService
{
    Task<TrainReservationResult> ReserveTrainForPassengerWithOrderAsync(
        TrainPassengerReserveRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<TrainReservationResult> ReserveTrainForCarWithOrderAsync(
        TrainCarReserveRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<TrainReservationData?> GetReservationDataAsync(Guid reservationId);
}

// DTOs moved to BuildingBlocks for reusability
public record TrainPassengerReserveRequest
{
    public string MainPassengerTel { get; init; } = string.Empty;
    public int CaptchaId { get; init; }
    public string CaptchVal { get; init; } = string.Empty;
    public string ReserveToken { get; init; } = string.Empty;
    public List<TrainPassengerRequest> Passengers { get; init; } = new();
    public bool IsExclusiveDepart { get; init; }
    public bool? IsExclusiveReturn { get; init; }
}
public record TrainCarReserveRequest
{
    public string MainPassengerTel { get; init; } = string.Empty;
    public int CaptchaId { get; init; }
    public string CaptchVal { get; init; } = string.Empty;
    public string ReserveToken { get; init; } = string.Empty;
    public List<TrainPassengerRequest> CarOwner { get; init; } = new();
    public CarInfoRequest CarInformation { get; init; } = new();
    public bool IsExclusiveDepart { get; init; }
    public bool? IsExclusiveReturn { get; init; }
}
public record CarInfoRequest
{
    public string CarModel { get; set; }
    public string PlakNumber { get; set; }
    public string CarModelName { get; set; }

}
public record TrainPassengerRequest
{
    public string Name { get; init; } = string.Empty;
    public string Family { get; init; } = string.Empty;
    public string BirthDatePersian { get; init; } = string.Empty;
    public string? Nationalcode { get; init; }
    public string? PassportNo { get; init; }
    public int DepartOptionalServiceCode { get; init; }
    public int? RetrunOptionalServiceCode { get; init; }
    public int DepartFreeServiceCode { get; init; }
    public int? ReturnFreeServiceCode { get; init; }
    public bool IsIranian { get; init; }
    public int Gender { get; set; }
}

public record TrainReservationResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public required string ReservationId { get; set; }
    public Guid? OrderId { get; set; }        
    public string? OrderNumber { get; set; }  
}
public record TrainReservationData
{
    public long OrderID { get; init; }
    public long OrderFullPrice { get; init; }
    public string ConfirmationToken { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
