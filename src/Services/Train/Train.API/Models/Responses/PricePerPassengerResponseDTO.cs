namespace Train.API.Models.Responses;
public class PricePerPassengerResponseDTO
{
    public decimal AdultPrice { get; set; }
    public decimal ChildPrice { get; set; }
    public decimal InfantPrice { get; set; }
    public decimal CoupeDeductionPrice { get; set; }
}
