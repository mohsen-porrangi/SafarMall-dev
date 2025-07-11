using BuildingBlocks.Extensions;
using Carter;
using FluentValidation;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Order.API.Middleware;
using Order.API.Services;
using Order.Application;
using Order.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddJwtAuthentication(builder.Configuration);

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Order.Infrastructure.Data.Context.OrderDbContext>("Database");
builder.Services.AddHttpClient();
// Infrastructure & Application
builder.Services.AddOrderInfrastructure(builder.Configuration);
builder.Services.AddOrderApplication(builder.Configuration);

// API
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddCarter();
builder.Services.AddEndpointsApiExplorer();

// CORS
builder.Services.AddAllowAllCors();

// Swagger
builder.Services.AddOpenApiWithJwt();

builder.Services.AddExceptionHandler<ErrorHandlerMiddleware>();
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCurrentUserService<CurrentUserService>();

var app = builder.Build();

// Configure pipeline
app.UseExceptionHandler();
app.UseStatusCodePages();

//if (app.Environment.IsDevelopment())
//{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Order API";
        options.Theme = ScalarTheme.Kepler;
        options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
//}

app.UseAuthentication();
app.UseCurrentUser();
app.UseAuthorization();

app.MapCarter();
app.UseCurrentUser();
app.UseHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();