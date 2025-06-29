namespace Train.API.Models.Responses;
public class ServicesReservedResponseDTO
{
    public int OptionalServiceCode { get; set; }
    public string OptionalServiceName { get; set; }
    public decimal OptionalServicePrice { get; set; }
    public int FreeServiceCode { get; set; }
    public string FreeServiceName { get; set; }
}
