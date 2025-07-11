namespace Train.API.Models.Requests;
public class IntermediateStationsInfoRequestDTO
{
    public int TrainNumber { get; set; }
    public int CircularPeriod { get; set; }
    public int CircularNumberSerial { get; set; }
    public DateTime MoveDate { get; set; }
}
