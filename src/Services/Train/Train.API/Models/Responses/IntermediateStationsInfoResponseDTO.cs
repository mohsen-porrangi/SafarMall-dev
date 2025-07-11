namespace Train.API.Models.Responses;
public class IntermediateStationsInfoResponseDTO
{
    public bool? IsStay { get; set; }
    public TimeSpan? ArriveTime { get; set; }
    public DateTime? ArriveDate { get; set; }
    public string? ArriveDatePersian { get; set; }
    public int? StationCode { get; set; }
    public int? TrainNumber { get; set; }
    public string Description { get; set; }
    public string StationName { get; set; }
}
