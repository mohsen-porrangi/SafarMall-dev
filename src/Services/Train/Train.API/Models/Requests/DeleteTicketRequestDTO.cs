namespace Train.API.Models.Requests;
public class DeleteTicketRequestDTO
{
    public int ReserveId { get; set; }
    public int TrainNumber { get; set; }
    public DateTime MoveDate { get; set; }
}
