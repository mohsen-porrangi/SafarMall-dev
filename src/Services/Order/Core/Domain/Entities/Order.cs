using BuildingBlocks.Contracts;
using BuildingBlocks.Domain;
using BuildingBlocks.Enums;
using Order.Domain.Enums;
using Order.Domain.Events;

namespace Order.Domain.Entities;

public class Order : EntityWithDomainEvents<Guid>, IAggregateRoot, ISoftDelete
{
    public Guid UserId { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public ServiceType ServiceType { get; private set; }
    public decimal TotalAmount { get; private set; }
    public OrderStatus LastStatus { get; private set; }
    public int PassengerCount { get; private set; }
    public bool HasReturn { get; private set; }
    public string? PaymentReference { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public virtual ICollection<OrderFlight> OrderFlights { get; private set; } = new List<OrderFlight>();
    public virtual ICollection<OrderTrain> OrderTrains { get; private set; } = new List<OrderTrain>();
    public virtual ICollection<OrderTrainCarTransport> OrderTrainCarTransports { get; private set; } = new List<OrderTrainCarTransport>();
    public virtual ICollection<OrderStatusHistory> StatusHistories { get; private set; } = new List<OrderStatusHistory>();
    public virtual ICollection<OrderWalletTransaction> WalletTransactions { get; private set; } = new List<OrderWalletTransaction>();

    // EF Constructor
    protected Order() { }

    public Order(Guid userId, string orderNumber, ServiceType serviceType, int passengerCount, bool hasReturn)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        OrderNumber = orderNumber;
        ServiceType = serviceType;
        PassengerCount = passengerCount;
        HasReturn = hasReturn;
        LastStatus = OrderStatus.Pending;
        TotalAmount = 0;
        CreatedAt = DateTime.UtcNow;

        AddDomainEvent(new OrderCreatedEvent(Id, UserId, ServiceType, orderNumber, TotalAmount));
    }

    public void UpdateStatus(OrderStatus newStatus, string reason = "")
    {
        if (LastStatus == newStatus) return;

        var history = new OrderStatusHistory(Id, LastStatus, newStatus, reason);
        StatusHistories.Add(history);

        var oldStatus = LastStatus;
        LastStatus = newStatus;
        UpdatedAt = DateTime.UtcNow;

        // Raise domain events based on status
        switch (newStatus)
        {
            case OrderStatus.Completed:
                AddDomainEvent(new OrderCompletedEvent(Id, UserId));
                break;
            case OrderStatus.Cancelled:
                AddDomainEvent(new OrderCancelledEvent(Id, UserId, reason));
                break;
        }
    }

    public void SetTotalAmount(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        TotalAmount = amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddWalletTransaction(long transactionId, OrderTransactionType type, decimal amount)
    {
        var transaction = new OrderWalletTransaction(Id, transactionId, type, amount);
        WalletTransactions.Add(transaction);
    }

    public bool CanBeCancelled()
    {
        return LastStatus == OrderStatus.Pending || LastStatus == OrderStatus.Processing;
    }
    /// <summary>
    /// Set payment reference (transaction ID or authority)
    /// </summary>
    public void SetPaymentReference(string? paymentReference)
    {
        // Add PaymentReference property to Order entity
        PaymentReference = paymentReference;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark order as paid
    /// </summary>
    public void MarkAsPaid()
    {
        UpdateStatus(OrderStatus.Paid, "پرداخت با موفقیت انجام شد");
        PaidAt = DateTime.UtcNow;

        // Raise domain event for payment completion
        AddDomainEvent(new OrderPaidEvent(
            Id,
            UserId,
            OrderNumber,
            ServiceType,
            TotalAmount,
            PaymentReference,
            PaidAt.Value));

    }

}