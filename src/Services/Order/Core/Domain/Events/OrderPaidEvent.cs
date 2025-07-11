using BuildingBlocks.Enums;
using BuildingBlocks.MessagingEvent.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Order.Domain.Events;
/// <summary>
/// Domain event raised when an order is successfully paid
/// Used for notifying other services (Train, Flight, etc.) to complete reservations
/// </summary>
public record OrderPaidEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public ServiceType ServiceType { get; init; }
    public decimal TotalAmount { get; init; }
    public string? PaymentReference { get; init; }
    public DateTime PaidAt { get; init; }    

    public OrderPaidEvent(
        Guid orderId,
        Guid userId,
        string orderNumber,
        ServiceType serviceType,
        decimal totalAmount,
        string? paymentReference,
        DateTime paidAt)
    {
        OrderId = orderId;
        UserId = userId;
        OrderNumber = orderNumber;
        ServiceType = serviceType;
        TotalAmount = totalAmount;
        PaymentReference = paymentReference;
        PaidAt = paidAt;
    }
}
