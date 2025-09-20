using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using NexaFlow.Application.Common.Interfaces;

namespace NexaFlow.Infrastructure.Services;

public class RedisCacheService(IDistributedCache distributedCache) : ICacheService
{
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var cachedResponse = await distributedCache.GetStringAsync(key, cancellationToken);
        return cachedResponse == null ? default : JsonSerializer.Deserialize<T>(cachedResponse);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpireTime = null, CancellationToken cancellationToken = default)
    {
        var response = JsonSerializer.Serialize(value);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpireTime ?? TimeSpan.FromMinutes(60)
        };
        await distributedCache.SetStringAsync(key, response, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await distributedCache.RemoveAsync(key, cancellationToken);
    }
}
