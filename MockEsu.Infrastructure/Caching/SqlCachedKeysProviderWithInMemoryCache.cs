using Microsoft.EntityFrameworkCore;
using MockEsu.Application.Common.Interfaces;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace MockEsu.Infrastructure.Caching;

internal class SqlCachedKeysProviderWithInMemoryCache : ICachedKeysProvider
{
    private readonly ICachedKeysContext _context;
    private readonly ConcurrentDictionary<string, CachedKeyData> _cachedKeys = [];

    public SqlCachedKeysProviderWithInMemoryCache(ICachedKeysContext context)
    {
        _context = context;
    }

    public async Task<bool> TryAddKeyToIdIfNotPresentAsync(string key, DateTimeOffset expires, Type entityType, int id)
    {
        var cachedKey = _cachedKeys.GetOrAdd(key, k => new CachedKeyData(expires));

        lock (cachedKey.TypeIdsPairs)
        {
            if (!cachedKey.TypeIdsPairs.TryGetValue(entityType.Name, out var ids))
            {
                cachedKey.TypeIdsPairs.Add(entityType.Name, ids = new HashSet<int>());
            }

            return ids.Add(id);
        }
    }

    public async Task<bool> TryCompleteFormationAsync(string key)
    {
        var list = ConvertToCachedKeyListAndCompleteFormation();
        await _context.CachedKeys.Where(k => list.Select(i => i.Key).Contains(k.Key)).ExecuteDeleteAsync();
        await _context.CachedKeys.AddRangeAsync(list);
        // string asd = JsonConvert.SerializeObject(_cachedKeys);
        return (await _context.SaveChangesAsync()) > 0;
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

    private List<CachedKey> ConvertToCachedKeyListAndCompleteFormation()
    {
        var list = new List<CachedKey>();

        foreach (var pair in _cachedKeys)
        {
            var cachedKey = new CachedKey
            {
                Key = pair.Key,
                Expires = pair.Value.Expires,
                FormationCompleted = true,
                TypeIdPairs = pair.Value.TypeIdsPairs.SelectMany(typePair =>
                    typePair.Value.Select(id => new TypeIdPair(id, typePair.Key))).ToHashSet()
            };

            list.Add(cachedKey);
        }

        return list;
    }


    private class CachedKeyData
    {
        public DateTimeOffset Expires { get; set; }
        public Dictionary<string, HashSet<int>> TypeIdsPairs { get; set; } = new Dictionary<string, HashSet<int>>();
        public bool FormationCompleted { get; set; } = false;
        public CachedKeyData(DateTimeOffset expires)
        {
            Expires = expires;
        }
    }
}
