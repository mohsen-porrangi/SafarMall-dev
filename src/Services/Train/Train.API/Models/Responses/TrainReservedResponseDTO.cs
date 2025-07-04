using static BuildingBlocks.Contracts.Services.IOrderExternalService;

namespace Train.API.Models.Responses;

//public class TrainReservedResponseDTO
//{
//    public required string MainPassengerTel { get; set; }
//    public bool IsExclusive { get; set; }
//    public decimal? ExclusiveAmount { get; set; }
//    public int SeatCount { get; set; }
//    public decimal FullPrice { get; set; }
//    public int TrainNumber { get; set; }
//    public int ReserveId { get; set; }
//    public int TicketType { get; set; }
//    public int ProviderId { get; set; }
//    public string SourceName { get; set; }
//    public string DestinationName { get; set; }
//    public DateTime DepartureDate { get; set; }
//    public DateTime ReturnDate { get; set; }
//    public string DepartureDatePersian { get; set; }
//    public string DepartureTime { get; set; }
//    public string WagonNumbers { get; set; }
//    public string CompartmentNumbers { get; set; }
//    public DateTime RequestDateTime { get; set; }
//    public required List<OrderPassengerInfo> Passengers { get; set; }

//    //public DateTime FinalDepartureTime =>
//    //    !string.IsNullOrEmpty(DepartureTime) && TimeSpan.TryParse(DepartureTime, out TimeSpan time)
//    //        ? DepartureDate.Add(time)
//    //        : DepartureDate;
//}

