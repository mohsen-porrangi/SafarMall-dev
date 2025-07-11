using Train.API.Models.Middle;

namespace Train.API.Models.Requests;

public class GenerateReserveKeyRequestDTO
{
    public AvalableTrainDTO DepartSelectedTrain { get; set; }
    public AvalableTrainDTO? ReturnSelectedTrain { get; set; }
}
