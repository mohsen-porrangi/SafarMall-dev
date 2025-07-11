using BuildingBlocks.Extensions;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using WalletApp.API.Middleware;
using WalletApp.API.Services;
using WalletApp.Application;
using WalletApp.Application.EventHandlers.External;
using WalletApp.Infrastructure;
using WalletApp.Infrastructure.Persistence.Context;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);
// CORS
builder.Services.AddAllowAllCors();

builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);
builder.Services.AddCurrentUserService<CurrentUserService>();

// Swagger
//builder.Services.AddSwaggerWithJwt(title: "Wallet Application API", version: "v1", description: "API برای مدیریت کیف پول", enableAuth: true);
builder.Services.AddOpenApiWithJwt();

// Add layers in correct order
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var appAssembly = typeof(UserActivatedEventHandler).Assembly;
Console.WriteLine($"[DEBUG] Application Assembly: {appAssembly.FullName}");
Console.WriteLine($"[DEBUG] Application Assembly Location: {appAssembly.Location}");

// Add Carter for minimal API endpoints
builder.Services.AddCarter();

// Add HTTP Context Accessor for CurrentUserService
builder.Services.AddHttpContextAccessor();

// Register API-specific CurrentUserService
//builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

Console.WriteLine("[DEBUG] AddMessaging loaded from: " + typeof(UserActivatedEventHandler).Assembly.FullName);


// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<WalletDbContext>("wallet-db");

var app = builder.Build();



app.UseCors("AllowAllOrigins");
//app.UseExceptionHandler();
app.UseStatusCodePages();
// Configure the HTTP request pipeline
//if (app.Environment.IsDevelopment())
//{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Wallet Application API";
        options.Theme = ScalarTheme.Kepler;
        options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
//}

// Custom Middleware
app.UseMiddleware<ErrorHandlerMiddleware>();


app.UseHttpsRedirection();
app.UseAuthentication();
app.UseCurrentUser();   
app.UseAuthorization();


// Carter endpoints
app.MapCarter();


// Health checks
app.UseHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});


// Database migration (Development only)
if (app.Environment.IsDevelopment())
{



    #region EventTest    
   
    #endregion

}


app.Run();