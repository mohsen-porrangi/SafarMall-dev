namespace Train.API.Models.Responses;

public class OptionalServiceTrainsResponseDTO
{
    public required List<OptionalServiceResponseDTO> DepartOptionalService { get; set; }
    public List<OptionalServiceResponseDTO>? ReturnOptionalService { get; set; }
}
