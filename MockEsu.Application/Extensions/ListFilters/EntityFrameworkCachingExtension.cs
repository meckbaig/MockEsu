using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace MockEsu.Application.Extensions.ListFilters;

public static class EntityFrameworkCachingExtension
{
    private static readonly DistributedCacheEntryOptions CacheEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
    };

    public static async Task<TResult> GetOrCreate<TResult>(
        this IDistributedCache cache,
        string key,
        Func<TResult> factory,
        CancellationToken cancellationToken = default,
        DistributedCacheEntryOptions? options = null)
    {
        string cachedMember = await cache.GetStringAsync(key, cancellationToken);
        if (!string.IsNullOrEmpty(cachedMember))
            return JsonConvert.DeserializeObject<TResult>(cachedMember);
        TResult result = factory.Invoke();
        await cache.SetStringAsync(key,
            JsonConvert.SerializeObject(result),
            options ?? CacheEntryOptions,
            cancellationToken);
        return result;
    }

    public static async Task<TResult> GetOrCreate<TResult>(
        this IDistributedCache cache,
        string key,
        Func<TResult> factory,
        TimeSpan? absoluteExpirationRelativeToNow,
        CancellationToken cancellationToken = default)
    {
        DistributedCacheEntryOptions options = new()
        {
            AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow
        };
        return await cache.GetOrCreate(
            key, 
            factory, 
            cancellationToken, 
            options);
    }
}