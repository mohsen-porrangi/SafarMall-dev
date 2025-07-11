namespace Train.API.Models.Middle;
public class AvalableTrainDTO
{
    public required AvalableTrainInfo Data { get; set; }    
    public required string OwnerImage { get; set; }
    public required string OwnerName { get; set; }
    public required string ReserveKey { get; set; }
    //public required string ReserveToken { get; set; }
    public required int Priority { get; set; }
}
