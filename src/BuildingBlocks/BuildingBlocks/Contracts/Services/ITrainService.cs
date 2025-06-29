namespace BuildingBlocks.Contracts.Services;

public interface ITrainService
{
    Task<TrainReservationResult> ReserveTrainWithEventAsync(
        TrainReserveRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<TrainReservationData?> GetReservationDataAsync(Guid reservationId);
}

// DTOs moved to BuildingBlocks for reusability
public record TrainReserveRequest
{
    public string MainPassengerTel { get; init; } = string.Empty;
    public int CaptchaId { get; init; }
    public string CaptchVal { get; init; } = string.Empty;
    public string ReserveToken { get; init; } = string.Empty;
    public List<TrainPassengerRequest> Passengers { get; init; } = new();
    public bool IsExclusiveDepart { get; init; }
    public bool? IsExclusiveReturn { get; init; }
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
}

public record TrainReservationResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public List<Guid> CreatedReservationIds { get; init; } = new();
}
public record TrainReservationData
{
    public Guid ReservationId { get; init; }
    public string ReserveToken { get; init; } = string.Empty;
    public int TrainNumber { get; init; }
    public int ReserveId { get; init; }
    public DateTime CreatedAt { get; init; }
}