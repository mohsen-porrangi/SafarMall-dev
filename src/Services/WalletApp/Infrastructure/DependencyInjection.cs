using BuildingBlocks.Contracts.Options;
using BuildingBlocks.Contracts.Services;
using BuildingBlocks.Extensions;
using BuildingBlocks.Messaging.Extensions;
using BuildingBlocks.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using WalletApp.Application.Common.Interfaces;
using WalletApp.Application.EventHandlers.External;
using WalletApp.Domain.Common.Contracts;
using WalletApp.Domain.DomainServices;
using WalletApp.Infrastructure.BackgroundServices;
using WalletApp.Infrastructure.ExternalServices;
using WalletApp.Infrastructure.HealthChecks;
using WalletApp.Infrastructure.Persistence;
using WalletApp.Infrastructure.Persistence.Context;
using WalletApp.Infrastructure.Persistence.Repositories;
using WalletApp.Infrastructure.Services;

namespace WalletApp.Infrastructure;

/// <summary>
/// Infrastructure layer dependency injection - Clean implementation
/// KISS: Simple, no duplicate registrations
/// </summary>
[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DependencyInjection))]
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database Configuration
        services.AddDatabase(configuration);

        // Repository & UoW Pattern
        services.AddRepositories();

        // Domain Services
        services.AddDomainServices();

        // External Services
        services.AddExternalServices();

        // Background Services
        services.AddBackgroundServices();

        // User Management Integration
        services.ExternalServiceRegister(configuration);

        // SIMPLE: One line messaging setup - NO DUPLICATES!
        services.AddMessaging(configuration,
            typeof(UserActivatedEventHandler).Assembly,  // WalletApp.Application
            typeof(DependencyInjection).Assembly);

        return services;
    }

    /// <summary>
    /// Configure database and DbContext
    /// </summary>
    private static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<WalletDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("WalletConnectionString"),
                b => b.MigrationsAssembly(typeof(WalletDbContext).Assembly.FullName));

            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(false);
        });

        services.AddScoped<IWalletDbContext>(provider =>
            provider.GetRequiredService<WalletDbContext>());

        return services;
    }

    /// <summary>
    /// Configure repositories and Unit of Work
    /// </summary>
    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        //services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }

    /// <summary>
    /// Configure domain services
    /// </summary>
    private static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<ITransactionDomainService, TransactionDomainService>();
        services.AddScoped<IFeeCalculationService, FeeCalculationService>();
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();

        return services;
    }

    /// <summary>
    /// Configure external services
    /// </summary>
    private static IServiceCollection AddExternalServices(this IServiceCollection services)
    {
        services.AddHttpClient<IPaymentGatewayClient, PaymentGatewayClient>();
        services.AddScoped<ICurrencyExchangeService, CurrencyExchangeService>();

        return services;
    }

    /// <summary>
    /// Configure background services
    /// </summary>
    private static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<AccountSnapshotBackgroundService>();
        services.AddHostedService<CreditDueDateCheckingService>();
        services.AddHostedService<WalletReconciliationService>();
        services.AddHostedService<WalletStartupRecoveryService>();

        services.AddHealthChecks()
            .AddCheck<WalletHealthCheck>("wallet-status");

        return services;
    }

    /// <summary>
    /// Configure User Management integration
    /// </summary>
    public static IServiceCollection ExternalServiceRegister(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddExternalService<IUserManagementService, UserManagementServiceClient, UserManagementOptions>(
            configuration, UserManagementOptions.SectionName);
        services.AddExternalService<IPaymentGatewayClient, PaymentGatewayClient, PaymentGatewayServiceOptions>(
            configuration, PaymentGatewayServiceOptions.SectionName);
        services.AddExternalService<IOrderServiceClient, OrderServiceClient, OrderServiceOptions>(
            configuration, OrderServiceOptions.SectionName);
        return services;
    }
}