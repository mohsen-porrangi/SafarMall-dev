namespace Train.API.Models.Requests;
public class SearchTrainRequestDTO
{
    public SearchPassengersRequestDTO? PassengerCount { get; set; }
    public int FromStationCode { get; set; }
    public int ToStationCode { get; set; }
    public string DepartPersianDate { get; set; }
    public bool isActiveBooking { get; set; } = false;
    public bool IsOneWay { get; set; } = true;
    public string? ReturnPersianDate { get; set; } = null;
    public bool? IsExclusiveDepart { get; set; }
    public bool? IsExclusiveReturn { get; set; }
    public int? SexCode { get; set; }
    public int TicketType { get; set; } = 1;
}
