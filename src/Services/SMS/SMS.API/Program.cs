using BuildingBlocks;
using BuildingBlocks.Contracts;
using BuildingBlocks.Middleware;
using BuildingBlocks.Utils.SafeLog.LogService;
using BuildingBlocks.Utils.SafeLog;
using SMS.API.Services;
using SMS.API.Models.OptionModels;
using Simple.Infrastructure.SharedService.Caching;

var builder = WebApplication.CreateBuilder(args);


#region SmsService
builder.Services.Configure<KavenegarSmsOptions>(
   builder.Configuration.GetSection(KavenegarSmsOptions.Name));
#endregion

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.BuildingBlocksInjection(builder.Configuration);

builder.Services.AddSingleton<IIntegrationService, IntegrationService>();
builder.Services.AddSingleton<MemoryCacheService>();
builder.Services.AddScoped<SmsService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddSwaggerGen();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

SafeLog.Configure(app.Services.GetRequiredService<SafeLogService>());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseMiddleware<RequestIdMiddleware>();

app.MapControllers();

app.Run();
