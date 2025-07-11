using BuildingBlocks.Contracts;
using BuildingBlocks.Enums;
using Order.Domain.Enums;

namespace Order.Domain.Entities;

public class OrderTrainCarTransport : OrderItem, ISoftDelete
{
    public string CarNumber { get; private set; } = string.Empty;
    public string CarName { get; private set; } = string.Empty;
    public decimal TransportAmount => TotalPrice;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    // Navigation
    public virtual Order Order { get; private set; } = null!;

    protected OrderTrainCarTransport() { }

    public OrderTrainCarTransport(
        Guid orderId,
        string firstNameEn, string lastNameEn,
        string firstNameFa, string lastNameFa,
        DateTime birthDate, Gender gender,
        bool isIranian, string? nationalCode, string? PassportNo,
        int sourceCode, int destinationCode,
        string sourceName, string destinationName,
        TicketDirection ticketDirection,
        DateTime departureTime, DateTime arrivalTime,
        string carNumber, string carName)
        : base(orderId, firstNameEn, lastNameEn, firstNameFa, lastNameFa,
               birthDate, gender, isIranian, nationalCode, PassportNo,
               sourceCode, destinationCode, sourceName, destinationName,
               ticketDirection, departureTime, arrivalTime)
    {
        CarNumber = carNumber;
        CarName = carName;
    }
}