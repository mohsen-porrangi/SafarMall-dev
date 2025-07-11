using BuildingBlocks.Contracts;
using BuildingBlocks.Enums;
using Order.Domain.Enums;

namespace Order.Domain.Entities;

public class OrderTrain : OrderItem, ISoftDelete
{
    public string TrainNumber { get; private set; } = string.Empty;
    public string WagonNumber { get; private set; } = string.Empty;
    public string CompartmentNumbers { get; private set; } = string.Empty;
    public bool IsExclusive { get; set; }
    public decimal? ExclusiveAmount { get; set; }

    //public TrainProvider ProviderId { get; private set; } TODO Change Provider method handle because it has complicated bessiness
    public int ProviderId { get; private set; }
    public decimal TrainAmount => TotalPrice; // Alias for consistency
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    // Navigation
    public virtual Order Order { get; private set; } = null!;

    protected OrderTrain() { }

    public OrderTrain(
        Guid orderId,
        string? firstNameEn, string? lastNameEn,
        string? firstNameFa, string? lastNameFa,
        DateTime birthDate, Gender gender,
        bool isIranian, string? nationalCode, string? PassportNo,
        int? sourceCode, int? destinationCode,
        string sourceName, string destinationName,
        TicketDirection ticketDirection,
        DateTime departureTime, DateTime arrivalTime,
        string trainNumber, /*TrainProvider*/ int providerId)
        : base(orderId, firstNameEn, lastNameEn, firstNameFa, lastNameFa,
               birthDate, gender, isIranian, nationalCode, PassportNo,
               sourceCode, destinationCode, sourceName, destinationName,
               ticketDirection, departureTime, arrivalTime)
    {
        TrainNumber = trainNumber;
        ProviderId = providerId;
    }
}