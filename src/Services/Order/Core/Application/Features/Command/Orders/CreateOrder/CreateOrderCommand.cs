using BuildingBlocks.CQRS;
using BuildingBlocks.Enums;

namespace Order.Application.Features.Command.Orders.CreateOrder;

public record CreateOrderCommand : ICommand<CreateOrderResult>
{
    public Guid UserId { get; init; }
    public ServiceType ServiceType { get; init; }
    public int? SourceCode { get; init; } = null;
    public int? DestinationCode { get; init; } = null;
    public string SourceName { get; init; } = string.Empty;
    public string DestinationName { get; init; } = string.Empty;
    public DateTime DepartureDate { get; init; }
    public DateTime? ReturnDate { get; init; }
    public List<CreateOrderPassengerInfo> Passengers { get; init; } = new();
    //  public List<CreateOrderItemInfo> Items { get; init; } = new();
    public string FlightNumber { get; init; } = string.Empty; // برای پرواز
    public string TrainNumber { get; init; } = string.Empty; // برای قطار
    public int ProviderId { get; init; } // شرکت ارائه‌دهنده
    public decimal BasePrice { get; init; }
}

public record CreateOrderPassengerInfo(
    string? FirstNameEn,
    string? LastNameEn,
    string? FirstNameFa,
    string? LastNameFa,
    DateTime BirthDate,
    Gender Gender,
    bool IsIranian,
    string? NationalCode,
    string? PassportNo
);

//public record CreateOrderItemInfo(
//    TicketDirection Direction,
//    DateTime DepartureTime,
//    DateTime ArrivalTime,
//    string ServiceNumber, // Flight or Train number
//    string Provider,
//    decimal BasePrice
//);

public record CreateOrderResult(
    Guid OrderId,
    string OrderNumber,
    decimal TotalAmount
);