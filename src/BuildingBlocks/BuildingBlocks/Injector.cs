using BuildingBlocks.Contracts;
using BuildingBlocks.Domain;
using BuildingBlocks.Utils.SafeLog.LogService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Simple.Application.Model.OptionPatternModels;
using StackExchange.Redis;

namespace BuildingBlocks;

public static class Injector
{
    public static IServiceCollection BuildingBlocksInjection(this IServiceCollection services, ConfigurationManager configuration)
    {
        #region Log
        services.Configure<LogOptions>(
           configuration.GetSection(LogOptions.Name));
        #endregion

        #region Redis
        services.Configure<RedisOptions>(
          configuration.GetSection(RedisOptions.Name));

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
            return ConnectionMultiplexer.Connect(options.ConnectionString);
        });
        #endregion

        #region mongo
        services.Configure<MongoOptions>(
          configuration.GetSection(MongoOptions.Name));

        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
            return new MongoClient(settings.ConnectionString);
        });
        #endregion

        //   services.AddSingleton<IIntegrationService, IntegrationService>();
        services.AddSingleton<SafeLogService>();
        services.AddSingleton<ILogService, LogService>();
        services.AddHttpContextAccessor();
        //    services.AddHttpClient();

        return services;
    }
}
