using static BuildingBlocks.Contracts.Services.IOrderExternalService;

namespace Train.API.Models.Responses;
public class ReserveResponseDTO
{
    public required List<TrainReservedDTO> Trains { get; set; }
    public string ReserveConfirmationToken { get; set; }
}


