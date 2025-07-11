using BuildingBlocks;
using BuildingBlocks.Contracts;
using BuildingBlocks.Contracts.Options;
using BuildingBlocks.Contracts.Services;
using BuildingBlocks.Extensions;
using BuildingBlocks.Utils.SafeLog;
using BuildingBlocks.Utils.SafeLog.LogService;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Simple.Infrastructure.SharedService.Caching;
using Train.API.ExternalServices;
using Train.API.Models.OptionalModels;
using Train.API.Services;

var builder = WebApplication.CreateBuilder(args);

var assembly = typeof(Program).Assembly;
// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApiWithJwt();

// CORS
builder.Services.AddAllowAllCors();

builder.Services.BuildingBlocksInjection(builder.Configuration);

builder.Services.Configure<TrainWrapper>(
           builder.Configuration.GetSection(TrainWrapper.Name));

builder.Services.AddSingleton<RajaServices>();
builder.Services.AddScoped<ITrainService, TrainReservationService>();
builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();

//TODO Add Handler
//builder.Services.AddMessaging(builder.Configuration, typeof(OrderFailedEventHandler).Assembly);
//Console.WriteLine("[DEBUG] AddMessaging loaded from: " + typeof(OrderFailedEventHandler).Assembly.FullName);

builder.Services.AddCurrentUserService<CurrentUserService>();

builder.Services.AddSingleton<IIntegrationService, IntegrationService>();
builder.Services.AddEndpointsApiExplorer();
// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Order Service
builder.Services.AddExternalService<IOrderExternalService, OrderServiceClient, OrderServiceOptions>(
        builder.Configuration, OrderServiceOptions.SectionName);


// Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

SafeLog.Configure(app.Services.GetRequiredService<SafeLogService>());

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "Trian API";
    options.Theme = ScalarTheme.Kepler;
    options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
});
//}


app.UseHttpsRedirection();

app.UseAuthorization();

//app.UseMiddleware<RequestIdMiddleware>();

app.MapControllers();

// Health checks
app.UseHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseCurrentUser();

app.Run();
