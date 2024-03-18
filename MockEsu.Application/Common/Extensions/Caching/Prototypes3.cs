using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Collections.Concurrent;

namespace MockEsu.Application.Common.Extensions.Caching;

public static class CachedKeys2
{
    private static ConcurrentDictionary<string, CachedKeyData> _cachedKeys = [];

    internal static bool TryAddKeyToIdIfNotPresent(string key, DateTimeOffset expires, Type entityType, int id)
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

    internal static bool CompleteFormation(string key)
    {
        if (!_cachedKeys.TryGetValue(key, out CachedKeyData cachedKeyData))
            return false;
        cachedKeyData.FormationCompleted = true;
        return true;
    }

    public static bool TryGetAndRemoveKeyById(Type entityType, int id, out string key)
    {
        key = null;
        ClearExpiredKeys();
        foreach (var pair in _cachedKeys)
        {
            if (pair.Value.TypeIdsPairs.TryGetValue(entityType, out var ids) && ids.Contains(id))
            {
                key = pair.Key;
                break;
            }
        }
        if (key == null)
            return false;
        return _cachedKeys.TryRemove(key, out _);
    }

    private static void ClearExpiredKeys()
    {
        foreach (var cachedKeyPair in _cachedKeys)
        {
            if (cachedKeyPair.Value.Expires < DateTimeOffset.UtcNow)
            {
                _cachedKeys.TryRemove(cachedKeyPair.Key, out _);
            }
        }
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