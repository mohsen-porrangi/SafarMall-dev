using BuildingBlocks.Enums;
using Train.API.Models.Enums;

namespace Train.API.Models.Responses;
public class PassengerReservedResponseDTO
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string BirthDatePersian { get; set; }
    public DateTime BirthDate { get; set; }
    public string? NationalCode { get; set; }
    public Gender Gender { get; set; }
    public string? PassportNo { get; set; }
    public ServicesReservedResponseDTO? Services { get; set; }
    public required decimal Amount { get; set; }
    public PassengerTypeEnum PassengerType { get; set; }
    public bool IsIranian { get; set; }
}

