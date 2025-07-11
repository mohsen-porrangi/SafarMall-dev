namespace Train.API.Models.Responses;
public class ConfirmingResrvedTicketResponseDTO
{
    public bool IsPurchased { get; set; }
    public string? Description { get; set; }
    public List<int>? TicketNumber { get; set; }
    public List<int>? TrainNumber { get; set; }
}
