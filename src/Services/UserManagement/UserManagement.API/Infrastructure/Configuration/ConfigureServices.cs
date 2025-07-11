using BuildingBlocks.Behaviors;
using BuildingBlocks.Contracts.Security;
using BuildingBlocks.Contracts.Services;
using BuildingBlocks.Messaging.Extensions;
using System.Reflection;
using UserManagement.API.AccessControl.Services;
using UserManagement.API.Infrastructure.Data;
using UserManagement.API.Services;

namespace UserManagement.API.Infrastructure.Configuration
{
    public static class ConfigureServices
    {
        private static Assembly assembly = typeof(Program).Assembly;

        public static void ConfigureSqlServer(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<UserDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("UserConnectionString")));
        }

        public static void ConfigureMediatR(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssembly(assembly);
                config.AddOpenBehavior(typeof(ValidationBehavior<,>));
                config.AddOpenBehavior(typeof(LoggingBehavior<,>));
                config.AddOpenBehavior(typeof(ExceptionHandlingBehavior<,>));
            });
        }

        public static void ConfigureService(this IServiceCollection services, IConfiguration configuration)
        {
            var walletBaseAddress = configuration.GetSection("WalletService:BaseAddress").Value
                                    ?? throw new InvalidOperationException("WalletService:BaseAddress not configured");

            // Domain Services
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();            
            services.AddScoped<IPermissionService, UserPermissionService>();
            services.AddScoped<IUserManagementService, UserManagementService>();
            services.AddScoped<ITemporaryRegistrationService, TemporaryRegistrationService>();
            // HTTP Clients
            services.AddHttpClient<IOtpService, OtpService>(client =>
            {
                client.BaseAddress = new Uri(walletBaseAddress);
            });

            // Caching
            services.AddMemoryCache();

            // Add assembly parameter for event handler registration
            services.AddMessaging(configuration, assembly);
        }
    }
}