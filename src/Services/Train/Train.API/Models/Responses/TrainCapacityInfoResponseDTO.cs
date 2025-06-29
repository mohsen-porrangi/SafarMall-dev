namespace Train.API.Models.Responses;
public class TrainCapacityInfoResponseDTO
{
    public int TotalRemainingCapacity { get; set; }
    public int MaximumPurchaseCount { get; set; }
    public bool IsExclusive { get; set; }
}
