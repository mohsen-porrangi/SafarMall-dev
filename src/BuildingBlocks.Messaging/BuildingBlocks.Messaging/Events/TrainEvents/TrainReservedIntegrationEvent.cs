using BuildingBlocks.Enums;
using BuildingBlocks.MessagingEvent.Base;
using BuildingBlocks.Models.DTOs;

namespace BuildingBlocks.Messaging.Events.TrainEvents;

/// <summary>
/// Event published when train reservation is completed successfully
/// Moved to BuildingBlocks for cross-service communication standardization
/// </summary>
public record TrainReservedIntegrationEvent : IntegrationEvent
{
    public Guid ReservationId { get; init; }
    public Guid UserId { get; init; }
    public string MainPassengerTel { get; init; } = string.Empty;
    public ServiceType ServiceType { get; init; } = ServiceType.Train;
    public int TrainNumber { get; init; }
    public int ProviderId { get; init; }
    public string SourceName { get; init; } = string.Empty;
    public string DestinationName { get; init; } = string.Empty;
    public DateTime DepartureDate { get; init; }
    public DateTime? ReturnDate { get; init; }
    public string DepartureDatePersian { get; init; } = string.Empty;
    public string DepartureTime { get; init; } = string.Empty;
    public decimal FullPrice { get; init; }
    public bool IsExclusive { get; init; }
    public decimal? ExclusiveAmount { get; init; }
    public int SeatCount { get; init; }
    public int TicketType { get; init; }
    public string WagonNumbers { get; init; } = string.Empty;
    public string CompartmentNumbers { get; init; } = string.Empty;
    public DateTime RequestDateTime { get; init; }
    public List<TrainPassengerInfo> Passengers { get; init; } = new();

    public TrainReservedIntegrationEvent(
        Guid reservationId,
        Guid userId,
        string mainPassengerTel,
        int trainNumber,
        int providerId,
        string sourceName,
        string destinationName,
        DateTime departureDate,
        DateTime? returnDate,
        string departureDatePersian,
        string departureTime,
        decimal fullPrice,
        bool isExclusive,
        decimal? exclusiveAmount,
        int seatCount,
        int ticketType,
        string wagonNumbers,
        string compartmentNumbers,
        DateTime requestDateTime,
        List<TrainPassengerInfo> passengers)
    {
        ReservationId = reservationId;
        UserId = userId;
        MainPassengerTel = mainPassengerTel;
        TrainNumber = trainNumber;
        ProviderId = providerId;
        SourceName = sourceName;
        DestinationName = destinationName;
        DepartureDate = departureDate;
        ReturnDate = returnDate;
        DepartureDatePersian = departureDatePersian;
        DepartureTime = departureTime;
        FullPrice = fullPrice;
        IsExclusive = isExclusive;
        ExclusiveAmount = exclusiveAmount;
        SeatCount = seatCount;
        TicketType = ticketType;
        WagonNumbers = wagonNumbers;
        CompartmentNumbers = compartmentNumbers;
        RequestDateTime = requestDateTime;
        Passengers = passengers;
        Source = "TrainService";
    }
}
