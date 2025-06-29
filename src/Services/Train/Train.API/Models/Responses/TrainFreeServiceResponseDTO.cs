namespace Train.API.Models.Responses;
public class TrainFreeServiceResponseDTO
{
    public required FreeServiceResponseDTO DepartFreeService { get; set; }
    public FreeServiceResponseDTO? ReturnFreeService { get; set; }
}
