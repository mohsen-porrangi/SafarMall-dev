using BuildingBlocks.Enums;
using BuildingBlocks.MessagingEvent.Base;

namespace Order.Domain.Events;
public record OrderCreatedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public ServiceType ServiceType { get; init; }
    public string? OrderNumber { get; init; }
    public decimal FullAmount { get; init; }

    public OrderCreatedEvent(
        Guid orderId,
        Guid userId,
        ServiceType serviceType,
        string? OrderNumber,
        decimal fullAmount)
    {
        OrderId = orderId;
        UserId = userId;
        ServiceType = serviceType;
        Source = "OrderService";
        OrderId = orderId;
        FullAmount = fullAmount;
    }
}