using BuildingBlocks.Behaviors;
using BuildingBlocks.Contracts.Services;
using BuildingBlocks.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using WalletApp.Application.Common.Behaviors;
using WalletApp.Application.Features.Services.Refunds;


namespace WalletApp.Application;

/// <summary>
/// Application layer dependency injection configuration
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR Registration with all behaviors
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Pipeline behaviors in order of execution
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ExceptionHandlingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        // FluentValidation Registration
        services.AddValidatorsFromAssembly(assembly);

        // Application Services
        services.AddApplicationServices();

        return services;
    }

    /// <summary>
    /// Register application-specific services
    /// </summary>
    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
    //    services.AddScoped<IUserManagementService, UserManagementServiceClient>();
        services.AddScoped<IRefundService, RefundService>();
        return services;
    }
}