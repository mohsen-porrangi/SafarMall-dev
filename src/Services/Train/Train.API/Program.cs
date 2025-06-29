using BuildingBlocks;
using BuildingBlocks.Contracts;
using BuildingBlocks.Contracts.Services;
using BuildingBlocks.Extensions;
using BuildingBlocks.Messaging.Extensions;
using BuildingBlocks.Middleware;
using BuildingBlocks.Utils.SafeLog;
using BuildingBlocks.Utils.SafeLog.LogService;
using Simple.Infrastructure.SharedService.Caching;
using System.Configuration;
using Train.API.EventHandlers;
using Train.API.Models.OptionalModels;
using Train.API.Services;

var builder = WebApplication.CreateBuilder(args);

var assembly = typeof(Program).Assembly;
// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// CORS
builder.Services.AddAllowAllCors();

builder.Services.BuildingBlocksInjection(builder.Configuration);

builder.Services.Configure<TrainWrapper>(
           builder.Configuration.GetSection(TrainWrapper.Name));

builder.Services.AddSingleton<RajaServices>();
builder.Services.AddScoped<ITrainService, TrainReservationService>();
builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();
builder.Services.AddMessaging(builder.Configuration, typeof(OrderFailedEventHandler).Assembly);

Console.WriteLine("[DEBUG] AddMessaging loaded from: " + typeof(OrderFailedEventHandler).Assembly.FullName);

builder.Services.AddCurrentUserService<CurrentUserService>();

builder.Services.AddSingleton<IIntegrationService, IntegrationService>();
builder.Services.AddEndpointsApiExplorer();
// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);
// Swagger
builder.Services.AddSwaggerWithJwt(title: "Train API", version: "v1", description: "API برای سرویس های قطار رجا", enableAuth: true);


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerExtention("Train API", "v1");
}

SafeLog.Configure(app.Services.GetRequiredService<SafeLogService>());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.UseHttpsRedirection();

app.UseAuthorization();

//app.UseMiddleware<RequestIdMiddleware>();

app.MapControllers();

app.UseCurrentUser();

app.Run();
