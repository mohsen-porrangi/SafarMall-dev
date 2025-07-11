using BuildingBlocks.Behaviors;
using BuildingBlocks.Contracts;
using BuildingBlocks.Contracts.Options;
using BuildingBlocks.Contracts.Services;
using BuildingBlocks.Extensions;
using BuildingBlocks.Messaging.Extensions;
using Carter;
using FluentValidation;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.API.BackgroundServices;
using PaymentGateway.API.Common.Contracts;
using PaymentGateway.API.Data;
using PaymentGateway.API.Middleware;
using PaymentGateway.API.Providers;
using PaymentGateway.API.Providers.Sandbox;
using PaymentGateway.API.Providers.ZarinPal;
using PaymentGateway.API.Providers.Zibal;
using PaymentGateway.API.Services;
using Scalar.AspNetCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Logging
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// CORS
builder.Services.AddAllowAllCors();

// OpenApi
builder.Services.AddOpenApiWithJwt();

// Database
builder.Services.AddDbContext<PaymentDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("PaymentGatewayConnectionString"));
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
});

// MediatR with Behaviors
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ExceptionHandlingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// Carter for minimal APIs
builder.Services.AddCarter();

// HTTP Context
builder.Services.AddHttpContextAccessor();

// Current User Service
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Repository and UoW
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IWebhookLogRepository, WebhookLogRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Payment Providers
builder.Services.AddScoped<ZarinPalProvider>();
builder.Services.AddScoped<ZibalProvider>();
builder.Services.AddScoped<SandboxProvider>();
builder.Services.AddScoped<IPaymentGatewayFactory, PaymentGatewayFactory>();

// HTTP Clients
builder.Services.AddHttpClient<ZarinPalClient>();
builder.Services.AddHttpClient<ZibalClient>();

// Wallet Service
builder.Services.AddExternalService<IWalletServiceClient, WalletServiceClient, WalletServiceOptions>(
        builder.Configuration, WalletServiceOptions.SectionName);

// Services
builder.Services.AddScoped<IWebhookProcessor, WebhookProcessor>();
builder.Services.AddScoped<IRetryService, RetryService>();
builder.Services.AddScoped<IWebhookProcessor, WebhookProcessor>();

builder.Services.AddOptions<PaymentGatewayOptions>()
    .Bind(builder.Configuration.GetSection(PaymentGatewayOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Background Services
if (builder.Configuration.GetValue<bool>("BackgroundServices:PaymentStatusCheck:Enabled"))
{
    builder.Services.AddHostedService<PaymentStatusCheckService>();
}

if (builder.Configuration.GetValue<bool>("BackgroundServices:RetryFailedPayments:Enabled"))
{
    builder.Services.AddHostedService<RetryFailedPaymentsService>();
}

// Memory Cache
builder.Services.AddMemoryCache();

builder.Services.AddMessaging(builder.Configuration, Assembly.GetExecutingAssembly());

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PaymentDbContext>("payment-db");

var app = builder.Build();

// Configure pipeline
app.UseCors("AllowAllOrigins");

//if (app.Environment.IsDevelopment())
//{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Payment Gateway API";
        options.Theme = ScalarTheme.Kepler;
        options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
//}

// Custom Middleware
app.UseMiddleware<ErrorHandlerMiddleware>();
app.UseMiddleware<WebhookSignatureMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Carter endpoints
app.MapCarter();

// Health checks
app.UseHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});


// Controllers
app.MapControllers();

// Database migration (Development only)
//if (app.Environment.IsDevelopment())
//{
//    using var scope = app.Services.CreateScope();
//    var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
//    await context.Database.MigrateAsync();
//}

app.Run();