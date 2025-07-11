using BuildingBlocks.Contracts.Options;
using BuildingBlocks.Extensions;
using BuildingBlocks.Messaging.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Order.Application.Common.Interfaces;
using Order.Application.EventHandlers;
using Order.Domain.Contracts;
using Order.Domain.Services;
using Order.Infrastructure.BackgroundServices;
using Order.Infrastructure.Data.Context;
using Order.Infrastructure.Data.Repositories;
using Order.Infrastructure.ExternalServices.Configuration;
using Order.Infrastructure.ExternalServices.Flight;
using Order.Infrastructure.ExternalServices.Train;
using Order.Infrastructure.ExternalServices.WalletPayment;
using Order.Infrastructure.Services;

namespace Order.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<OrderDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("OrderConnectionString"),
                b => b.MigrationsAssembly(typeof(OrderDbContext).Assembly.FullName)));

        services.AddScoped<IOrderDbContext>(provider => provider.GetService<OrderDbContext>()!);

        // Repositories
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderFlightRepository, OrderFlightRepository>();
        services.AddScoped<IOrderTrainRepository, OrderTrainRepository>();
        services.AddScoped<IOrderTrainCarTransportRepository, OrderTrainCarTransportRepository>(); //  اضافه شد
        services.AddScoped<ISavedPassengerRepository, SavedPassengerRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Domain Services
        services.AddScoped<IOrderNumberGenerator, OrderNumberGenerator>();
        services.AddScoped<OrderPricingService>();

        // Application Services
        // services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IDateTimeService, DateTimeService>();
        services.AddScoped<ITicketService, TicketService>();

        // External Services
        services.Configure<ExternalServicesOptions>(configuration.GetSection("ExternalServices"));


        services.AddScoped<IFlightService, FlightServiceClient>();
        services.AddScoped<ExternalServices.Train.ITrainService, TrainServiceClient>();

        // Background Services
        services.AddHostedService<OrderExpirationService>();

        // Messaging
        services.AddMessaging(configuration, typeof(PaymentCompletedEventHandler).Assembly);

        // Add External Services
        services.ExternalServiceRegister(configuration);

        return services;
    }
    public static IServiceCollection ExternalServiceRegister(this IServiceCollection services, IConfiguration configuration)
    {
        // Wallet Service       
        services.AddExternalService<IWalletServiceClient, WalletServiceClient, WalletServiceOptions>(
            configuration, WalletServiceOptions.SectionName);

        return services;
    }
}