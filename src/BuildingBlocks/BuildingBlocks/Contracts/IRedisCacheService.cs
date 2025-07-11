using BuildingBlocks.Enums;

namespace BuildingBlocks.Contracts;
public interface IRedisCacheService
{
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, BusinessPrefixKeyEnum? prefix = null);
    Task SetHashAsync<T>(string hashKey, string field, T value);
    Task<T?> GetAsync<T>(string key, BusinessPrefixKeyEnum? prefix = null);
    Task<bool> ExistsAsync(string key, BusinessPrefixKeyEnum? prefix = null);
    Task RemoveAsync(string key, BusinessPrefixKeyEnum? prefix = null);
    Task RefreshAsync(string key, TimeSpan? expiration = null, BusinessPrefixKeyEnum? prefix = null);
    Task ClearByPrefixAsync(BusinessPrefixKeyEnum prefix);
    Task<bool> ApplyEffortLimit(string uniqKey, BusinessPrefixKeyEnum serviceLimitation, int maxAttempts, int timeWindowInSeconds = 120);
    Task<long> IncrementAsync(string key, BusinessPrefixKeyEnum? prefix = null);
}
