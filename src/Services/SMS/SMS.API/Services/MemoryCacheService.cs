using Microsoft.Extensions.Caching.Memory;

namespace Simple.Infrastructure.SharedService.Caching;

public class MemoryCacheService 
{
    private readonly MemoryCache _cache;

    public MemoryCacheService()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    public void SetValue(string key, object value, TimeSpan ttl)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };

        _cache.Set(key, value, cacheEntryOptions);
    }

    public object GetValue(string key)
    {
        _cache.TryGetValue(key, out var value);
        return value;
    }
    public void RemoveValue(string key)
    {
        _cache.Remove(key);
    }
    public bool CanAttemptVerification(string phoneNumber, int maxAttempts = 3, int timeWindowInSeconds = 120)
    {
        string attemptKey = $"attempts_{phoneNumber}";

        // بررسی تعداد تلاش‌ های موجود در کش
        var attempts = GetValue(attemptKey) as int? ?? 0;

        if (attempts >= maxAttempts)
        {
            // تعداد تلاش‌ها به حداکثر رسیده است
            return false;
        }

        // افزایش تعداد تلاش‌ها و تنظیم TTL برای محدودیت زمانی
        attempts++;
        SetValue(attemptKey, attempts, TimeSpan.FromSeconds(timeWindowInSeconds));

        return true;
    }
}