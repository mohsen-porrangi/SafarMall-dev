using BuildingBlocks.Contracts;
using BuildingBlocks.Enums;
using Order.Domain.Enums;

namespace Order.Domain.Entities;

public class OrderFlight : OrderItem , ISoftDelete
{
    public string FlightNumber { get; private set; } = string.Empty;
    public FlightProvider ProviderId { get; private set; }
    public decimal FlightAmount => TotalPrice; // Alias for consistency
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    // Navigation
    public virtual Order Order { get; private set; } = null!;

    protected OrderFlight() { }

    public OrderFlight(
        Guid orderId,
        string firstNameEn, string lastNameEn,
      //  string firstNameFa, string lastNameFa,
        DateTime birthDate, Gender gender,
        bool isIranian, string? nationalCode, string? PassportNo,
        int? sourceCode, int? destinationCode,
        string sourceName, string destinationName,
        TicketDirection ticketDirection,
        DateTime departureTime, DateTime arrivalTime,
        string flightNumber, FlightProvider providerId)
        : base(orderId, firstNameEn, lastNameEn, null, null,
               birthDate, gender, isIranian, nationalCode, PassportNo,
               sourceCode, destinationCode, sourceName, destinationName,
               ticketDirection, departureTime, arrivalTime)
    {
        FlightNumber = flightNumber;
        ProviderId = providerId;
    }
}