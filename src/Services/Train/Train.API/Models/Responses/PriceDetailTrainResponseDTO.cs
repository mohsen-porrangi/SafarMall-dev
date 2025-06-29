namespace Train.API.Models.Responses;
public class PriceDetailTrainResponseDTO
{
    public PricePerPassengerResponseDTO? IranianPassengersPrice { get; set; }
    public PricePerPassengerResponseDTO? NonIranianPassengersPrice { get; set; }
    public TrainCapacityInfoResponseDTO? TrainInformation { get; set; }
    public int? CarTransferPrice { get; set; }
}
