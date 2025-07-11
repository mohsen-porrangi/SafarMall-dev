using BuildingBlocks;
using BuildingBlocks.Contracts;
using BuildingBlocks.Extensions;
using BuildingBlocks.Middleware;
using BuildingBlocks.Utils.SafeLog;
using BuildingBlocks.Utils.SafeLog.LogService;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Simple.Infrastructure.SharedService.Caching;
using SMS.API.Models.OptionModels;
using SMS.API.Services;

var builder = WebApplication.CreateBuilder(args);


#region SmsService
builder.Services.Configure<KavenegarSmsOptions>(
   builder.Configuration.GetSection(KavenegarSmsOptions.Name));
#endregion

// Add services to the container.

builder.Services.AddControllers();


builder.Services.BuildingBlocksInjection(builder.Configuration);

builder.Services.AddSingleton<IIntegrationService, IntegrationService>();
builder.Services.AddSingleton<MemoryCacheService>();
builder.Services.AddScoped<SmsService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
// CORS
builder.Services.AddAllowAllCors();
// OpenApi
builder.Services.AddOpenApiWithJwt();

// Health Checks
builder.Services.AddHealthChecks();    

var app = builder.Build();


// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "SMS API";
        options.Theme = ScalarTheme.Kepler;
        options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
//}

SafeLog.Configure(app.Services.GetRequiredService<SafeLogService>());



app.UseHttpsRedirection();

app.UseAuthorization();

app.UseMiddleware<RequestIdMiddleware>();

app.MapControllers();
// Health checks
app.UseHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
