using AutoMapper.Internal;
using Microsoft.Extensions.Caching.Distributed;
using MockEsu.Domain.Common;
using Newtonsoft.Json;
using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace MockEsu.Application.Common.Extensions.Caching;

public static class CachingExtension
{
    private static readonly DistributedCacheEntryOptions CacheEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(3000)
    };

    /// <summary>
    /// Gets data from cache if present; otherwise, executes factory and saves in cache.
    /// </summary>
    /// <typeparam name="TResult">Result type.</typeparam>
    /// <param name="cache">Caching provider.</param>
    /// <param name="key">Key for saving data.</param>
    /// <param name="requestResultFactory">Execution factory with operation to get data.</param>
    /// <param name="options">Options for caching provider.</param>
    /// <param name="cancellationToken"></param>
    /// <returns><typeparamref name="TResult"/> from <paramref name="requestResultFactory"/> or from cache.</returns>
    public static async Task<TDto> GetOrCreateAsync<TResult, TDto>(
        this IDistributedCache cache,
        string key,
        Func<TResult> requestResultFactory,
        Func<TResult, TDto> projectionFactory,
        CancellationToken cancellationToken = default,
        DistributedCacheEntryOptions? options = null)
    {
        string cachedMember = await cache.GetStringAsync(key, cancellationToken);
        if (!string.IsNullOrEmpty(cachedMember))
            return JsonConvert.DeserializeObject<TDto>(cachedMember);

        TResult requestResult = requestResultFactory.Invoke();
        options ??= CacheEntryOptions;
        
        await TrackIds(requestResult, key, 
            DateTimeOffset.UtcNow.Add(options.AbsoluteExpirationRelativeToNow ?? TimeSpan.Zero));

        TDto dtoResult = projectionFactory.Invoke(requestResult);
        await cache.SetStringAsync(key,
            JsonConvert.SerializeObject(dtoResult),
            options,
            cancellationToken);
        return dtoResult;
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
    public static async Task<TDto> GetOrCreateAsync<TResult, TDto>(
        this IDistributedCache cache,
        string key,
        Func<TResult> factory,
        Func<TResult, TDto> projectionFactory,
        TimeSpan? absoluteExpirationRelativeToNow,
        CancellationToken cancellationToken = default)
    {
        DistributedCacheEntryOptions options = new()
        {
            AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow
        };
        return await cache.GetOrCreateAsync(
            key,
            factory,
            projectionFactory,
            cancellationToken,
            options);
    }

    private static async Task TrackIds<TResult>(TResult result, string key, DateTimeOffset expires)
    {
        Type resultType = typeof(TResult);
        await TrackIdsInternal(result, resultType, key, expires);
        CachedKeys2.CompleteFormation(key);
    }

    private static async Task TrackIdsInternal(object result, Type resultType, string key, DateTimeOffset expires)
    {
        if (resultType.IsCollection())
        {
            IEnumerable resultCollection = (IEnumerable)result;
            if (resultCollection == null)
                return;
            Type collectionElementType = resultType.GetGenericArguments().Single();
            foreach (var collectionElement in resultCollection)
            {
                await TrackIdsInternal(collectionElement, collectionElementType, key, expires);
            }
        }
        else if (typeof(BaseEntity).IsAssignableFrom(resultType))
        {
            var idProperty = resultType.GetProperty(nameof(BaseEntity.Id));
            int idValue = (int)idProperty.GetValue(result);
            if (CachedKeys2.TryAddKeyToIdIfNotPresent(key, expires, resultType, idValue))
            {
                foreach (var property in resultType.GetProperties())
                {
                    if (property.PropertyType.IsCollection() || typeof(BaseEntity).IsAssignableFrom(property.PropertyType))
                    {
                        var value = property.GetValue(result);
                        if (value != null)
                            await TrackIdsInternal(value, property.PropertyType, key, expires);
                    }
                }
            }
        }
    }

    public static void RemoveFromCache(
        this IDistributedCache cache,
        string key)
    {
        cache.Remove(key);
    }
}