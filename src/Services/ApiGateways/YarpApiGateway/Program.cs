using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using YarpApiGateway.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddGatewayServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
app.ConfigureGatewayPipeline();
app.UseHealthChecks("/healthz", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.Run();