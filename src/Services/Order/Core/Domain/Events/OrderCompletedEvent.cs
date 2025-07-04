using BuildingBlocks.MessagingEvent.Base;

namespace Order.Domain.Events;

public record OrderCompletedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }

    public OrderCompletedEvent(Guid orderId, Guid userId)
    {
        OrderId = orderId;
        UserId = userId;
        Source = "OrderService";
    }
}