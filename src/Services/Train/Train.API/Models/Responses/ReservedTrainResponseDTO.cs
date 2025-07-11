using Train.API.Models.Middle;

namespace Train.API.Models.Responses;
public class ReservedTrainResponseDTO
{
    public required AvalableTrainInfo DepartTrain { get; set; }
    public AvalableTrainInfo? ReturnTrain { get; set; }
    public DateTime RequestDateTime { get; set; }
}
