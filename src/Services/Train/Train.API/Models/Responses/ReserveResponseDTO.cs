using BuildingBlocks.Models.DTOs;
using System.Text.Json.Serialization;

namespace Train.API.Models.Responses;
public class ReserveResponseDTO
{
    public required List<TrainReservedDTO> Trains { get; set; }
    public string ReserveConfirmationToken { get; set; }
}


public record TrainReservedDTO
{
    public string MainPassengerTel;
    public bool IsExclusive;
    public decimal? ExclusiveAmount;
    public int SeatCount;
    [JsonPropertyName("FullPrice")]
    public decimal TotalPrice;
    public int TrainNumber;
    public int ReserveId;
    public int TicketType;
    [JsonPropertyName("ownerCode")]
    public int ProviderId;
    public string SourceName;
    public string DestinationName;
    [JsonPropertyName("moveDate")]
    public DateTime DepartureDate;
    public DateTime? ReturnDate;
    [JsonPropertyName("moveDatePersian")]
    public string DepartureDatePersian;
    [JsonPropertyName("moveTime")]
    public string DepartureTime;
    public string WagonNumbers;
    public string CompartmentNumbers;
    public DateTime RequestDateTime;
    public List<OrderPassengerInfo> Passengers;

    public DateTime FinalDepartureTime =>
        !string.IsNullOrEmpty(DepartureTime) && TimeSpan.TryParse(DepartureTime, out TimeSpan time)
            ? DepartureDate.Add(time)
            : DepartureDate;
}