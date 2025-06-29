namespace Train.API.Models.Requests;
public class PassengerReserveRequestDTO
{
    public required string Name { get; set; }
    public required string Family { get; set; }
    public required string BirthDatePersian { get; set; }
    public string? Nationalcode { get; set; }
    public string? PassportNo { get; set; }
    public int DepartOptionalServiceCode { get; set; }
    public int? RetrunOptionalServiceCode { get; set; }
    public int DepartFreeServiceCode { get; set; }
    public int? ReturnFreeServiceCode { get; set; }
    public bool IsIranian { get; set; }
}
