using BuildingBlocks.Contracts;
using BuildingBlocks.Enums;
using BuildingBlocks.Extensions;
using Microsoft.Extensions.Options;
using Simple.Application.Model.OptionPatternModels;
using StackExchange.Redis;

namespace Simple.Infrastructure.SharedService.Caching;
public class RedisCacheService : IRedisCacheService
{
    private readonly IDatabase _database;
    private readonly IServer _server;
    private readonly string _keyPrefix;

    public RedisCacheService(IConnectionMultiplexer connectionMultiplexer, IOptions<RedisOptions> redisOptions)
    {
        _database = connectionMultiplexer.GetDatabase();
        var endpoints = connectionMultiplexer.GetEndPoints();
        _server = connectionMultiplexer.GetServer(endpoints.First());

        _keyPrefix = redisOptions.Value.KeyPrefix;
    }

    private string AddPrefix(string key, BusinessPrefixKeyEnum? customPrefix = null)
    {
        var prefix = customPrefix.ToString() ?? _keyPrefix;
        return $"{prefix}:{key}";
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, BusinessPrefixKeyEnum? prefix = null)
    {
        try
        {
            var json = value!.ToJson();
            await _database.StringSetAsync(AddPrefix(key, prefix), json, expiration);
        }
        catch (Exception ex)
        {
            // Log Exception
        }
    }

    public async Task SetHashAsync<T>(string hashKey, string field, T value)
    {
        var json = value!.ToJson();
        await _database.HashSetAsync(hashKey, new HashEntry[] { new HashEntry(field, json) });
    }



    public async Task<T?> GetAsync<T>(string key, BusinessPrefixKeyEnum? prefix = null)
    {
        try
        {
            var json = await _database.StringGetAsync(AddPrefix(key, prefix));
            if (json.IsNullOrEmpty) return default;
            return json!.ToString().JsonToType<T>();
        }
        catch
        {
            // Log Exception
            return default;
        }
    }

    public async Task<bool> ExistsAsync(string key, BusinessPrefixKeyEnum? prefix = null)
    {
        try
        {
            return await _database.KeyExistsAsync(AddPrefix(key, prefix));
        }
        catch
        {
            // Log Exception
            return false;
        }
    }

    public async Task RemoveAsync(string key, BusinessPrefixKeyEnum? prefix = null)
    {
        try
        {
            await _database.KeyDeleteAsync(AddPrefix(key, prefix));
        }
        catch
        {
            // Log Exception
        }
    }

    public async Task RefreshAsync(string key, TimeSpan? expiration = null, BusinessPrefixKeyEnum? prefix = null)
    {
        try
        {
            var value = await _database.StringGetAsync(AddPrefix(key, prefix));
            if (!value.IsNullOrEmpty)
            {
                await _database.StringSetAsync(AddPrefix(key, prefix), value, expiration);
            }
        }
        catch
        {
            // Log Exception
        }
    }

    public async Task ClearByPrefixAsync(BusinessPrefixKeyEnum prefix)
    {
        try
        {
            var completePrefix = AddPrefix(string.Empty, prefix);
            foreach (var key in _server.Keys(pattern: $"{completePrefix}*"))
            {
                await _database.KeyDeleteAsync(key);
            }
        }
        catch
        {
            // Log Exception
        }
    }

    public async Task<bool> ApplyEffortLimit(string uniqKey, BusinessPrefixKeyEnum serviceLimitation, int maxAttempts, int timeWindowInSeconds = 120)
    {
        // بررسی تعداد تلاش‌ های موجود در کش
        var attempts = await GetAsync<int>(uniqKey, BusinessPrefixKeyEnum.OverlimitSendOTP);

        if (attempts >= maxAttempts)
        {
            // تعداد تلاش‌ها به حداکثر رسیده است
            return false;
        }

        // افزایش تعداد تلاش‌ها و تنظیم TTL برای محدودیت زمانی
        attempts++;
        await SetAsync(uniqKey, attempts, TimeSpan.FromSeconds(timeWindowInSeconds), BusinessPrefixKeyEnum.OverlimitSendOTP);

        return true;
    }
    public async Task<long> IncrementAsync(string key, BusinessPrefixKeyEnum? prefix = null)
    {
        return await _database.StringIncrementAsync(AddPrefix(key, prefix));
    }
}
