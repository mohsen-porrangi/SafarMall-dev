using BuildingBlocks.Enums;
using System.Text.Json.Serialization;

namespace BuildingBlocks.Contracts.Services
{
    public interface IOrderExternalService
    {
        Task<bool> CreateTrainOrderAsync(CreateOrderInternalRequest request, CancellationToken cancellationToken);

        public record CreateOrderResponse(bool Success);
        public class CreateOrderInternalRequest()
        {
            public required List<TrainReservedDTO> Trains { get; set; }
            public string ReserveConfirmationToken { get; set; }
        }


        public record OrderPassengerInfo(
            [property: JsonPropertyName("name")] string FirstName,
            [property: JsonPropertyName("family")] string LastName,
            DateTime BirthDate,
            Gender Gender,
            bool IsIranian,
            string? NationalCode,
            string? PassportNo
        );
        public record TrainReservedDTO
        {
            public string MainPassengerTel;
            public bool IsExclusive;
            public decimal? ExclusiveAmount;
            public int SeatCount;
            public decimal FullPrice;
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
    }

}
