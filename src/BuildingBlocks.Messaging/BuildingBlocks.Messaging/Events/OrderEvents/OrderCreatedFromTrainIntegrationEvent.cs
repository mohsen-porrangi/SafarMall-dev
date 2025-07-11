using BuildingBlocks.Enums;
using BuildingBlocks.MessagingEvent.Base;

namespace BuildingBlocks.Messaging.Events.OrderEvents;

/// <summary>
/// Event published when order is successfully created from train reservation
/// Moved to BuildingBlocks for consistency across services
/// </summary>
public record OrderCreatedFromTrainIntegrationEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public ServiceType ServiceType { get; init; }
    public decimal TotalAmount { get; init; }
    public Guid TrainReservationId { get; init; }
    public int TrainNumber { get; init; }

    public OrderCreatedFromTrainIntegrationEvent(
        Guid orderId,
        Guid userId,
        string orderNumber,
        ServiceType serviceType,
        decimal totalAmount,
        Guid trainReservationId,
        int trainNumber)
    {
        OrderId = orderId;
        UserId = userId;
        OrderNumber = orderNumber;
        ServiceType = serviceType;
        TotalAmount = totalAmount;
        TrainReservationId = trainReservationId;
        TrainNumber = trainNumber;
        Source = "OrderService";
    }
}