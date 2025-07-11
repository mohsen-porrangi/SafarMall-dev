using BuildingBlocks.Contracts.Options;
using BuildingBlocks.Contracts.Services;
using BuildingBlocks.Enums;
using BuildingBlocks.Models.DTOs;
using BuildingBlocks.Services;
using Microsoft.Extensions.Options;

namespace Train.API.ExternalServices;

/// <summary>
/// Order Service HTTP client implementation
/// SOLID: Single Responsibility - only handles Order Service communication
/// DRY: Reuses AuthorizedHttpClient base functionality
/// KISS: Simple, focused implementation
/// </summary>
public sealed class OrderServiceClient(
    HttpClient httpClient,
    ILogger<OrderServiceClient> logger,
    IOptions<OrderServiceOptions> options,
    IHttpContextAccessor httpContextAccessor)
    : AuthorizedHttpClient(httpClient, logger, httpContextAccessor), IOrderExternalService
{
    private readonly OrderServiceOptions _options = options.Value;

    /// <summary>
    /// Create train order in Order Service
    /// </summary>
    public async Task<CreateTrainOrderResponse> CreateTrainOrderAsync(
        CreateTrainOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Creating train order: Passengers={PassengerCount}, Amount={Amount}",
            request.Passengers.Count, request.BasePrice);

        try
        {
            var internalRequest = OrderRequestMapper.ToInternal(request);
            var response = await PostAsync<CreateTrainOrderInternalRequest, CreateTrainOrderInternalResponse>(
                _options.Endpoints.CreateTrainOrder,
                internalRequest,
                cancellationToken);

            return response != null
                ? OrderResponseMapper.ToExternal(response)
                : OrderResponseMapper.CreateFailure("دریافت پاسخ از سرویس سفارش ناموفق بود");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create train order");
            return OrderResponseMapper.CreateFailure("خطا در ایجاد سفارش در سیستم");
        }
    }

    /// <summary>
    /// Get order details by ID
    /// </summary>
    public async Task<CreateTrainOrderResponse?> GetOrderDetailsAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Getting order details: OrderId={OrderId}", orderId);

        try
        {
            var endpoint = _options.Endpoints.GetOrderDetails.Replace("{orderId}", orderId.ToString());
            var response = await GetAsync<CreateTrainOrderInternalResponse>(endpoint, cancellationToken);

            return response != null ? OrderResponseMapper.ToExternal(response) : null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get order details for {OrderId}", orderId);
            return null;
        }
    }

    /// <summary>
    /// Update order status
    /// </summary>
    public async Task<bool> UpdateOrderStatusAsync(
        Guid orderId,
        string status,
        string reason = "",
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Updating order status: OrderId={OrderId}, Status={Status}", orderId, status);

        try
        {
            var request = new UpdateOrderStatusRequest(status, reason);
            var endpoint = _options.Endpoints.UpdateOrderStatus.Replace("{orderId}", orderId.ToString());

            var response = await PostAsync<UpdateOrderStatusRequest, object>(
                endpoint,
                request,
                cancellationToken);

            return response != null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update order status for {OrderId}", orderId);
            return false;
        }
    }

    /// <summary>
    /// Cancel order
    /// </summary>
    public async Task<bool> CancelOrderAsync(
        Guid orderId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Cancelling order: OrderId={OrderId}, Reason={Reason}", orderId, reason);

        try
        {
            var request = new CancelOrderRequest(reason);
            var endpoint = _options.Endpoints.CancelOrder.Replace("{orderId}", orderId.ToString());

            var response = await PostAsync<CancelOrderRequest, object>(
                endpoint,
                request,
                cancellationToken);

            return response != null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to cancel order {OrderId}", orderId);
            return false;
        }
    }
}

#region Internal DTOs - YAGNI: Only what we need

/// <summary>
/// Internal request format matching Order Service API
/// </summary>
internal record CreateTrainOrderInternalRequest(
    ServiceType ServiceType,
    int? SourceCode,
    int? DestinationCode,
    string SourceName,
    string DestinationName,
    int SeatCount,
    int TicketType,
    decimal TotalPrice,
    DateTime DepartureDate,
    DateTime? ReturnDate,
    List<CreateOrderPassengerInfo> Passengers,
    string? FlightNumber,
    string? TrainNumber,
    int ProviderId
);

/// <summary>
/// Internal response format from Order Service
/// </summary>
internal record CreateTrainOrderInternalResponse
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public ServiceType ServiceType { get; init; }
    public string SourceName { get; init; } = string.Empty;
    public string DestinationName { get; init; } = string.Empty;
    public DateTime DepartureDate { get; init; }
    public DateTime? ReturnDate { get; init; }
    public int PassengerCount { get; init; }
    public List<CreateOrderPassengerInfo>? Passengers { get; init; }
}

/// <summary>
/// Update order status request
/// </summary>
internal record UpdateOrderStatusRequest(string Status, string Reason = "");

/// <summary>
/// Cancel order request
/// </summary>
internal record CancelOrderRequest(string Reason);

#endregion

#region Mappers - SOLID: Single Responsibility for mapping logic

/// <summary>
/// SOLID: Single Responsibility - handles only request mapping
/// DRY: Centralized mapping logic
/// </summary>
internal static class OrderRequestMapper
{
    public static CreateTrainOrderInternalRequest ToInternal(CreateTrainOrderRequest request)
    {
        return new CreateTrainOrderInternalRequest(
            ServiceType: request.ServiceType,
            SourceCode: request.SourceCode,
            DestinationCode: request.DestinationCode,
            SourceName: request.SourceName,
            DestinationName: request.DestinationName,
            SeatCount: request.SeatCount,
            TicketType: request.TicketType,
            TotalPrice: request.BasePrice,
            DepartureDate: request.DepartureDate,
            ReturnDate: request.ReturnDate,
            Passengers: request.Passengers.Select(PassengerMapper.ToInternal).ToList(),
            FlightNumber: null,
            TrainNumber: request.TrainNumber,
            ProviderId: request.ProviderId
        );
    }
}

/// <summary>
/// SOLID: Single Responsibility - handles only response mapping
/// </summary>
internal static class OrderResponseMapper
{
    public static CreateTrainOrderResponse ToExternal(CreateTrainOrderInternalResponse response)
    {
        return new CreateTrainOrderResponse
        {
            Success = true,
            OrderId = response.OrderId,
            OrderNumber = response.OrderNumber,
            TotalAmount = response.TotalAmount,
            Status = "Pending",
            ServiceType = response.ServiceType,
            SourceName = response.SourceName,
            DestinationName = response.DestinationName,
            DepartureDate = response.DepartureDate,
            ReturnDate = response.ReturnDate,
            PassengerCount = response.PassengerCount,
            CreatedAt = DateTime.UtcNow,
            Passengers = response.Passengers?.Select(PassengerMapper.ToExternal).ToList()
        };
    }

    public static CreateTrainOrderResponse CreateFailure(string errorMessage)
    {
        return new CreateTrainOrderResponse
        {
            Success = false,
            ErrorMessage = errorMessage,
            CreatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// SOLID: Single Responsibility - handles only passenger mapping
/// DRY: Reusable passenger mapping logic
/// </summary>
internal static class PassengerMapper
{
    public static CreateOrderPassengerInfo ToInternal(TrainPassengerInfo passenger)
    {
        return new CreateOrderPassengerInfo(
            FirstNameEn: passenger.FirstNameEn,
            LastNameEn: passenger.LastNameEn,
            FirstNameFa: passenger.FirstNameFa,
            LastNameFa: passenger.LastNameFa,
            BirthDate: passenger.BirthDate,
            Gender: passenger.Gender,
            IsIranian: passenger.IsIranian,
            NationalCode: passenger.NationalCode,
            PassportNo: passenger.PassportNo
        );
    }

    public static TrainPassengerInfo ToExternal(CreateOrderPassengerInfo passenger)
    {
        return new TrainPassengerInfo
        {
            FirstNameEn = passenger.FirstNameEn ?? string.Empty,
            LastNameEn = passenger.LastNameEn ?? string.Empty,
            FirstNameFa = passenger.FirstNameFa ?? string.Empty,
            LastNameFa = passenger.LastNameFa ?? string.Empty,
            BirthDate = passenger.BirthDate,
            Gender = passenger.Gender,
            IsIranian = passenger.IsIranian,
            NationalCode = passenger.NationalCode,
            PassportNo = passenger.PassportNo
        };
    }
}

#endregion