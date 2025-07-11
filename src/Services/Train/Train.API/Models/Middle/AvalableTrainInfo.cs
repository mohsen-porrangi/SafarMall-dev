namespace Train.API.Models.Middle;
public class AvalableTrainInfo
{
    public bool IsOneWay { get; set; }
    public bool IsActiveBooking { get; set; }
    public string? ReasonDeactive { get; set; }
    public int SourceCode { get; set; }
    public int DestinationCode { get; set; }
    public string SourceStationName { get; set; }
    public string DestinationStationName { get; set; }
    public int SexCode { get; set; }
    public bool IsCarTransport { get; set; }
    public int? RetStatus { get; set; }
    public uint? TrainNumber { get; set; }
    public int? WagonType { get; set; }
    public string? WagonName { get; set; }
    public int? PathCode { get; set; }
    public int? CircularPeriod { get; set; }
    public DateTime? MoveDate { get; set; }
    public string? MoveDatePersian { get; set; }
    public DateTime? ExitDate { get; set; }
    public string? ExitDatePersian { get; set; }
    public string? ExitTime { get; set; }
    public int? Counting { get; set; }
    public int? SoldCount { get; set; }
    public int? Degree { get; set; }
    public int? Cost { get; set; }
    public int? FullPrice { get; set; }
    public int? CompartmentCapicity { get; set; }
    public int? IsCompartment { get; set; }
    public int? CircularNumberSerial { get; set; }
    public int? RateCode { get; set; }
    public bool? AirConditioning { get; set; }
    public bool? Media { get; set; }
    public string? TimeOfArrival { get; set; }
    public int? RationCode { get; set; }
    public int OwnerCode { get; set; }
}
