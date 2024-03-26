using MockEsu.Application.Common.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Infrastructure.Caching;

public class InMemoryCachedKeysProvider : ICachedKeysProvider
{
    private readonly ConcurrentDictionary<string, CachedKey> _cachedKeys = [];

    public async Task<bool> TryAddKeyToIdIfNotPresentAsync(string key, DateTimeOffset expires, Type entityType, int id)
    {
        var cachedKey = _cachedKeys.GetOrAdd(key, k => new CachedKey(expires));

        lock (cachedKey.TypeIdsPairs)
        {
            if (!cachedKey.TypeIdsPairs.TryGetValue(entityType, out var ids))
            {
                cachedKey.TypeIdsPairs.Add(entityType, ids = new HashSet<int>());
            }

            return ids.Add(id);
        }
    }

    public async Task<bool> TryCompleteFormationAsync(string key)
    {
        if (!_cachedKeys.TryGetValue(key, out CachedKey cachedKeyData))
            return false;
        cachedKeyData.FormationCompleted = true;
        return true;
    }

    public async Task<List<string>> GetAndRemoveKeysByIdAsync(Type entityType, int id)
    {
        await ClearExpiredKeys();
        List<string> keys = new List<string>();
        foreach (var pair in _cachedKeys)
        {
            if (pair.Value.TypeIdsPairs.TryGetValue(entityType, out var ids) && ids.Contains(id))
            {
                keys.Add(pair.Key);
            }
        }
        foreach (var key in keys)
        {
            _cachedKeys.TryRemove(key, out _);
        }
        return keys;
    }

    private async Task ClearExpiredKeys()
    {
        foreach (var cachedKeyPair in _cachedKeys)
        {
            if (cachedKeyPair.Value.Expires < DateTimeOffset.UtcNow)
            {
                _cachedKeys.TryRemove(cachedKeyPair.Key, out _);
            }
        }
    }

    private class CachedKey
    {
        public DateTimeOffset Expires { get; set; }
        public Dictionary<Type, HashSet<int>> TypeIdsPairs { get; set; } = new Dictionary<Type, HashSet<int>>();
        public bool FormationCompleted { get; set; } = false;
        public CachedKey(DateTimeOffset expires)
        {
            Expires = expires;
        }
    }
}
