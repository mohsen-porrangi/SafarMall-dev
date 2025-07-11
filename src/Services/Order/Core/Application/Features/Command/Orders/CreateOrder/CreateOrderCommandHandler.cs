using BuildingBlocks.Contracts;
using BuildingBlocks.CQRS;
using BuildingBlocks.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Order.Domain.Contracts;
using Order.Domain.Entities;
using Order.Domain.Enums;
using Order.Domain.Services;
using System.Configuration;
using static System.Collections.Specialized.BitVector32;

namespace Order.Application.Features.Command.Orders.CreateOrder;

public class CreateOrderCommandHandler(
    IUnitOfWork unitOfWork,
    IOrderNumberGenerator orderNumberGenerator,
    OrderPricingService pricingService,
    IConfiguration configuration,
 //   ICurrentUserService currentUserService,
    ILogger<CreateOrderCommandHandler> logger)
    : ICommandHandler<CreateOrderCommand, CreateOrderResult>
{
    public async Task<CreateOrderResult> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating new order for user {UserId}", request.UserId);

        // تولید شماره سفارش
        var orderNumber = await orderNumberGenerator.GenerateAsync(cancellationToken);

        // ایجاد سفارش
        var order = new Domain.Entities.Order(
            request.UserId,
            orderNumber,
            request.ServiceType,
            request.Passengers.Count,
            request.ReturnDate.HasValue
        );

        await unitOfWork.Orders.AddAsync(order, cancellationToken);

        // محاسبه و ایجاد OrderItems از روی Passengers
        decimal totalAmount = 0;
        var serviceNumbers = GetServiceNumbers(request);
        var providers = GetProviders(request);

        foreach (var passenger in request.Passengers)
        {
            var ageGroup = CalculateAgeGroup(passenger.BirthDate);

            // برای هر جهت (رفت و احتمالاً برگشت)
            var directions = GetDirections(request.ReturnDate.HasValue);

            foreach (var direction in directions)
            {
                var (departureTime, arrivalTime) = GetTimes(request.DepartureDate, request.ReturnDate, direction);
                var pricing = pricingService.CalculateTotalPrice(
                    request.ServiceType,
                    new(request.BasePrice, CurrencyCode.IRR),
                    ageGroup
                );
                var (taxRate, feeRate) = GetRates(request.ServiceType);
                var tax = pricing.Value * taxRate;
                var fee = pricing.Value * feeRate;
                switch (request.ServiceType)
                {
                    case ServiceType.DomesticFlight:
                    case ServiceType.InternationalFlight:
                        var flight = new OrderFlight(
                            order.Id,
                            passenger.FirstNameEn, passenger.LastNameEn,
                       //     passenger.FirstNameFa, passenger.LastNameFa,
                            passenger.BirthDate, passenger.Gender,
                            passenger.IsIranian, passenger.NationalCode, passenger.PassportNo,
                            request.SourceCode, request.DestinationCode,
                            request.SourceName, request.DestinationName,
                            direction,
                            departureTime, arrivalTime,
                            request.FlightNumber,
                            (FlightProvider)request.ProviderId
                        );
    
                        flight.SetPricing(pricing.Value, tax, fee);
                        await unitOfWork.OrderFlights.AddAsync(flight, cancellationToken);
                        break;

                    case ServiceType.Train:
                        var train = new OrderTrain(
                            order.Id,
                            passenger.FirstNameEn, passenger.LastNameEn,
                            passenger.FirstNameFa, passenger.LastNameFa,
                            passenger.BirthDate, passenger.Gender,
                            passenger.IsIranian, passenger.NationalCode, passenger.PassportNo,
                            request.SourceCode, request.DestinationCode,
                            request.SourceName, request.DestinationName,
                            direction,
                            departureTime, arrivalTime,
                            request.TrainNumber,
                            request.ProviderId
                        );


                        train.SetPricing(pricing.Value, tax, fee);
                        await unitOfWork.OrderTrains.AddAsync(train, cancellationToken);
                        break;
                }

                totalAmount += pricing.Value;
            }
        }

        // بروزرسانی مبلغ کل
        order.SetTotalAmount(totalAmount);

        var pending = unitOfWork.Context.ChangeTracker.Entries()
        .Where(e => e.State != EntityState.Unchanged)
        .ToList();

        logger.LogInformation("Pending changes: {Count}", pending.Count);

        var result = await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Order {OrderNumber} created successfully with total amount {TotalAmount}",
            orderNumber, totalAmount);

        return new CreateOrderResult(order.Id, orderNumber, totalAmount);
    }
    private (decimal taxRate, decimal feeRate) GetRates(ServiceType serviceType)
    {
        var section = serviceType switch
        {
            ServiceType.Train => "OrderPricing:Train",
            ServiceType.DomesticFlight or ServiceType.InternationalFlight => "OrderPricing:Flight",
            _ => "OrderPricing:Default"
        };

        var taxRate = configuration.GetValue<decimal>($"{section}:TaxRate");
        var feeRate = configuration.GetValue<decimal>($"{section}:FeeRate");

        return (taxRate, feeRate);
    }

    private static AgeGroup CalculateAgeGroup(DateTime birthDate)
    {
        var age = DateTime.Today.Year - birthDate.Year;
        if (birthDate.Date > DateTime.Today.AddYears(-age)) age--;

        return age switch
        {
            < 2 => AgeGroup.Infant,
            < 12 => AgeGroup.Child,
            _ => AgeGroup.Adult
        };
    }

    private static List<TicketDirection> GetDirections(bool hasReturn)
    {
        var directions = new List<TicketDirection> { TicketDirection.Outbound };
        if (hasReturn)
            directions.Add(TicketDirection.Inbound);
        return directions;
    }

    private static (DateTime departure, DateTime arrival) GetTimes(DateTime departureDate, DateTime? returnDate, TicketDirection direction)
    {
        if (direction == TicketDirection.Outbound)
        {
            return (departureDate, departureDate.AddHours(2)); // موقت - باید از سرویس خارجی گرفته شود
        }
        else
        {
            var retDate = returnDate ?? departureDate.AddDays(1);
            return (retDate, retDate.AddHours(2)); // موقت - باید از سرویس خارجی گرفته شود
        }
    }

    private static string[] GetServiceNumbers(CreateOrderCommand request)
    {
        return request.ServiceType switch
        {
            ServiceType.Train => [request.TrainNumber],
            _ => [request.FlightNumber]
        };
    }

    private static int[] GetProviders(CreateOrderCommand request)
    {
        return [request.ProviderId];
    }
}