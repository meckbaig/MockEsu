using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Infrastructure.AsyncMessaging;

namespace MockEsu.Infrastructure.Caching;

internal class AsyncSqlCachedKeysProvider : ICachedKeysProvider
{
    private readonly ICachedKeysContext _context;
    private CachedKeyData _cachedKey;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public AsyncSqlCachedKeysProvider(ICachedKeysContext context, IServiceScopeFactory serviceScopeFactory)
    {
        _context = context;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<bool> TryAddKeyToIdIfNotPresentAsync(string key, DateTimeOffset expires, Type entityType, int id)
    {
        _cachedKey ??= new CachedKeyData(key, expires);

        lock (_cachedKey.TypeIdsPairs)
        {
            if (!_cachedKey.TypeIdsPairs.TryGetValue(entityType.Name, out var ids))
            {
                _cachedKey.TypeIdsPairs.Add(entityType.Name, ids = new HashSet<int>());
            }
            return ids.Add(id);
        }
    }

    public async Task<bool> TryCompleteFormationAsync(string key)
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            var message = new CachedKeyDataMessage { CachedKey = _cachedKey };
            await publishEndpoint.Publish(message);
        }
        return true;
    }

    internal static async Task<bool> TryCompleteFormationAsync(CachedKeyData cachedKeyData, ICachedKeysContext context)
    {
        var cachedKey = ConvertToCachedKeyListAndCompleteFormation(cachedKeyData);
        await context.CachedKeys.Where(k => cachedKey.Key.Contains(k.Key)).ExecuteDeleteAsync();
        await context.CachedKeys.AddAsync(cachedKey);
        // string asd = JsonConvert.SerializeObject(_cachedKeys);
        return (await context.SaveChangesAsync()) > 0;
    }

    public async Task<List<string>> GetAndRemoveKeysByIdAsync(Type entityType, int id)
    {
        await ClearExpiredKeys();
        string asd = entityType.Name;
        List<CachedKey> cachedKeys = await _context.CachedKeys
            .Where(k => k.TypeIdPairs.Any(pair => pair.Type == entityType.Name && pair.EntityId == id))
            .ToListAsync();
        _context.CachedKeys.RemoveRange(cachedKeys);
        await _context.SaveChangesAsync();

        return cachedKeys.Select(k => k.Key).ToList();
    }

    private async Task ClearExpiredKeys()
    {
        await _context.CachedKeys.Where(k => k.Expires < DateTimeOffset.UtcNow).ExecuteDeleteAsync();
    }

    private static CachedKey ConvertToCachedKeyListAndCompleteFormation(CachedKeyData cachedKeyData)
    {
        var cachedKey = new CachedKey
        {
            Key = cachedKeyData.Key,
            Expires = cachedKeyData.Expires,
            FormationCompleted = true,
            TypeIdPairs = cachedKeyData.TypeIdsPairs.SelectMany(typePair =>
                typePair.Value.Select(id => new TypeIdPair(id, typePair.Key))).ToHashSet()
        };
        return cachedKey;
    }

    internal class CachedKeyData
    {
        public string Key { get; set; }
        public DateTimeOffset Expires { get; set; }
        public Dictionary<string, HashSet<int>> TypeIdsPairs { get; set; } = new Dictionary<string, HashSet<int>>();
        public bool FormationCompleted { get; set; } = false;
        public CachedKeyData(string key, DateTimeOffset expires)
        {
            Key = key;
            Expires = expires;
        }
    }
}
