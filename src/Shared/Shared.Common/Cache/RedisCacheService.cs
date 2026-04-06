using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Shared.Common.Cache;

public class RedisCacheService(IDistributedCache cache) : ICacheService
{
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<T?> GetAsync<T>(string key)
    {
        var data = await cache.GetStringAsync(key);
        return data is null ? default : JsonSerializer.Deserialize<T>(data, _jsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(30)
        };
        await cache.SetStringAsync(key, JsonSerializer.Serialize(value, _jsonOptions), options);
    }

    public async Task RemoveAsync(string key) => await cache.RemoveAsync(key);
}
