using Train.API.Models.Middle;

namespace Train.API.Models.Responses;
public record TrainSearchResponseDTO
{
    public required long searchId { get; set; }
    public required List<AvalableTrainDTO> DepartResult { get; set; }
    public List<AvalableTrainDTO>? ReturnResult { get; set; }
}
