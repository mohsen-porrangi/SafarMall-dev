namespace BuildingBlocks.Models.DTOs;

/// <summary>
/// Train reservation details shared between services
/// Moved to BuildingBlocks to standardize train reservation data structure
/// </summary>
public record TrainReservationInfo
{
    public int TrainNumber { get; init; }
    public string SourceName { get; init; } = string.Empty;
    public string DestinationName { get; init; } = string.Empty;
    public DateTime DepartureDate { get; init; }
    public DateTime? ReturnDate { get; init; }
    public string DepartureDatePersian { get; init; } = string.Empty;
    public string DepartureTime { get; init; } = string.Empty;
    public string WagonNumbers { get; init; } = string.Empty;
    public string CompartmentNumbers { get; init; } = string.Empty;
    public bool IsExclusive { get; init; }
    public decimal? ExclusiveAmount { get; init; }
    public int SeatCount { get; init; }
    public decimal FullPrice { get; init; }
    public int ProviderId { get; init; }
    public int TicketType { get; init; }
    public int ReserveId { get; init; }
    public DateTime RequestDateTime { get; init; }
}