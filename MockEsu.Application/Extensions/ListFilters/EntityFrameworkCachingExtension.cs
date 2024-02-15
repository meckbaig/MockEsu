using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace MockEsu.Application.Extensions.ListFilters;

public static class EntityFrameworkCachingExtension
{
    private static readonly DistributedCacheEntryOptions CacheEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
    };

    /// <summary>
    /// Gets data from cache if present; otherwise, executes factory and saves in cache.
    /// </summary>
    /// <typeparam name="TResult">Result type.</typeparam>
    /// <param name="cache">Caching provider.</param>
    /// <param name="key">Key for saving data.</param>
    /// <param name="factory">Execution factory with operation to get data.</param>
    /// <param name="options">Options for caching provider.</param>
    /// <param name="cancellationToken"></param>
    /// <returns><typeparamref name="TResult"/> from <paramref name="factory"/> or from cache.</returns>
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

    /// <summary>
    /// Gets data from cache if present; otherwise, executes factory and saves in cache.
    /// </summary>
    /// <typeparam name="TResult">Result type.</typeparam>
    /// <param name="cache">Caching provider.</param>
    /// <param name="key">Key for saving data.</param>
    /// <param name="factory">Execution factory with operation to get data.</param>
    /// <param name="absoluteExpirationRelativeToNow">Caching time.</param>
    /// <param name="cancellationToken"></param>
    /// <returns><typeparamref name="TResult"/> from <paramref name="factory"/> or from cache.</returns>
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