using BuildingBlocks.Enums;
using BuildingBlocks.Models.DTOs;

namespace BuildingBlocks.Contracts.Services;

/// <summary>
/// Order Service client interface for train reservation integration
/// Moved to BuildingBlocks for reusability across services
/// </summary>
public interface IOrderExternalService
{
    /// <summary>
    /// Create train order in Order Service
    /// </summary>
    Task<CreateTrainOrderResponse> CreateTrainOrderAsync(
        CreateTrainOrderRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get order details by ID
    /// </summary>
    Task<CreateTrainOrderResponse?> GetOrderDetailsAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update order status
    /// </summary>
    Task<bool> UpdateOrderStatusAsync(
        Guid orderId,
        string status,
        string reason = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel order
    /// </summary>
    Task<bool> CancelOrderAsync(
        Guid orderId,
        string reason,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Train order creation request
/// </summary>
public record CreateTrainOrderRequest
{
    public ServiceType ServiceType { get; init; } = ServiceType.Train;
    public int? SourceCode { get; init; }
    public int? DestinationCode { get; init; }
    public string SourceName { get; init; } = string.Empty;
    public string DestinationName { get; init; } = string.Empty;
    public DateTime DepartureDate { get; init; }
    public DateTime? ReturnDate { get; init; }
    public List<TrainPassengerInfo> Passengers { get; init; } = new();
    public string? TrainNumber { get; init; }
    public int ProviderId { get; init; }
    public decimal BasePrice { get; init; }
    public int SeatCount { get; init; }
    public int TicketType { get; init; }
}

/// <summary>
/// Complete train order response with all details for Redis caching
/// </summary>
public record CreateTrainOrderResponse
{
    public bool Success { get; init; }
    public Guid? OrderId { get; init; }
    public string? OrderNumber { get; init; }
    public decimal TotalAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public ServiceType ServiceType { get; init; }
    public string SourceName { get; init; } = string.Empty;
    public string DestinationName { get; init; } = string.Empty;
    public DateTime DepartureDate { get; init; }
    public DateTime? ReturnDate { get; init; }
    public int PassengerCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? ErrorMessage { get; init; }

    // Complete order data for Redis storage
    public List<OrderTrainInfo>? Trains { get; init; }
    public List<TrainPassengerInfo>? Passengers { get; init; }
}

/// <summary>
/// Order train information
/// </summary>
public record OrderTrainInfo
{
    public string TrainNumber { get; init; } = string.Empty;
    public string SourceName { get; init; } = string.Empty;
    public string DestinationName { get; init; } = string.Empty;
    public DateTime DepartureTime { get; init; }
    public DateTime? ArrivalTime { get; init; }
    public decimal BasePrice { get; init; }
    public decimal ServiceFee { get; init; }
    public decimal TaxAmount { get; init; }
    public string? TicketNumber { get; init; }
    public string Direction { get; init; } = string.Empty;
}