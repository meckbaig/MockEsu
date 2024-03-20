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
    private readonly ConcurrentDictionary<string, CachedKeyData> _cachedKeys = [];

    public async Task<bool> TryAddKeyToIdIfNotPresentAsync(string key, DateTimeOffset expires, Type entityType, int id)
    {
        var cachedKey = _cachedKeys.GetOrAdd(key, k => new CachedKeyData(expires));

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
        if (!_cachedKeys.TryGetValue(key, out CachedKeyData cachedKeyData))
            return false;
        cachedKeyData.FormationCompleted = true;
        return true;
    }

    public async Task<string?> GetAndRemoveKeyByIdAsync(Type entityType, int id)
    {
        string? key = null;
        await ClearExpiredKeys();
        foreach (var pair in _cachedKeys)
        {
            if (pair.Value.TypeIdsPairs.TryGetValue(entityType, out var ids) && ids.Contains(id))
            {
                key = pair.Key;
                break;
            }
        }
        if (key == null)
            return null;
        _cachedKeys.TryRemove(key, out _);
        return key;
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

    public bool TryGetAndRemoveKeyById(Type entityType, int id, out string key)
    {
        key = GetAndRemoveKeyByIdAsync(entityType, id).Result;
        if (key == null)
            return false;
        return true;
    }

    private class CachedKeyData
    {
        public DateTimeOffset Expires { get; set; }
        public Dictionary<Type, HashSet<int>> TypeIdsPairs { get; set; } = new Dictionary<Type, HashSet<int>>();
        public bool FormationCompleted { get; set; } = false;
        public CachedKeyData(DateTimeOffset expires)
        {
            Expires = expires;
        }
    }
}
