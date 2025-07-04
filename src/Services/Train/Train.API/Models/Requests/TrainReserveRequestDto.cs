namespace Train.API.Models.Requests;
public class TrainReserveRequestDto
{
    public required string MainPassengerTel { get; set; }
    public int CaptchaId { get; set; }
    public required string CaptchVal { get; set; }
    public required string ReserveToken { get; set; }
    public required List<PassengerReserveRequestDTO> Passengers { get; set; }
    public bool IsExclusiveDepart { get; set; }
    public bool? IsExclusiveReturn { get; set; }
}
