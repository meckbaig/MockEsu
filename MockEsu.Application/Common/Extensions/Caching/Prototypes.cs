namespace MockEsu.Application.Common.Extensions.Caching;

internal static class CachedIds
{
    private static Dictionary<Type, HashSet<CachedId>> _cachedIds { get; set; } = [];

    internal static void AddKeyToIdsByEntity(string key, DateTimeOffset expires, Type entityType, int[] ids)
    {
        CachedKey cachedKey = new() { Key = key, Expires = expires };
        AddPairIfNotPresent(entityType);
        foreach (var id in ids)
        {
            AddCachedKeyToIdByEntity(cachedKey, entityType, id);
        }
    }

    internal static void AddKeyToIdByEntity(string key, DateTimeOffset expires, Type entityType, int id)
    {
        CachedKey cachedKey = new() { Key = key, Expires = expires };
        AddPairIfNotPresent(entityType);
        AddCachedKeyToIdByEntity(cachedKey, entityType, id);
    }

    internal static IEnumerable<string> GetRemoveKeys(Type entityType, int id)
    {
        UpdateKeys(entityType);
        AddPairIfNotPresent(entityType);
        var keys = _cachedIds[entityType].FirstOrDefault(x => x.Id == id)?.Keys?.Select(k => k.Key) ?? [];
        RemoveKeys(keys);
        return keys;
    }

    internal static bool TryAddKeyToIdIfNotPresent(string key, DateTimeOffset expires, Type entityType, int id)
    {
        if (KeyByIdInTypeExists(key, entityType, id))
            return false;
        //Console.WriteLine($"{entityType.Name} with id:{id}");
        CachedKey cachedKey = new() { Key = key, Expires = expires };
        AddPairIfNotPresent(entityType);
        AddCachedKeyToIdByEntity(cachedKey, entityType, id);
        return true;
    }

    private static void AddCachedKeyToIdByEntity(CachedKey cachedKey, Type entityType, int id)
    {
        CachedId? cachedId = _cachedIds[entityType].FirstOrDefault(x => x.Id == id);
        if (cachedId == null)
        {
            cachedId = new() { Id = id, Keys = [cachedKey] };
            _cachedIds[entityType].Add(cachedId);
        }
        else
            cachedId.Keys.Add(cachedKey);
    }

    private static void RemoveKeys(IEnumerable<string> keys)
    {
        var cachedIdsToRemove = _cachedIds.Values
            .SelectMany(cachedIdSet => cachedIdSet
                .Where(cachedId => cachedId.Keys
                    .Any(cachedKey => keys.Contains(cachedKey.Key))));

        foreach (CachedId? cachedId in cachedIdsToRemove.ToList())
        {
            cachedId.Keys.RemoveWhere(cachedKey => keys.Contains(cachedKey.Key));

            if (cachedId.Keys.Count == 0)
            {
                foreach (HashSet<CachedId> cachedIdSet in _cachedIds.Values)
                {
                    cachedIdSet.Remove(cachedId);
                }
            }
        }
    }

    private static void UpdateKeys(Type entityType)
    {
        if (_cachedIds.TryGetValue(entityType, out var cachedIdSet))
        {
            foreach (var cachedId in cachedIdSet.ToList())
            {
                cachedId.Keys.RemoveWhere(cachedKey => cachedKey.Expires < DateTimeOffset.Now);

                if (cachedId.Keys.Count == 0)
                {
                    cachedIdSet.Remove(cachedId);
                }
            }
        }
    }

    private static void AddPairIfNotPresent(Type entityType)
    {
        if (!_cachedIds.ContainsKey(entityType))
            _cachedIds.Add(entityType, new HashSet<CachedId>());
    }

    private static bool KeyByIdInTypeExists(string key, Type entityType, int id)
    {
        if (_cachedIds.TryGetValue(entityType, out var cachedIdSet))
        {
            return cachedIdSet.Any(cachedId => cachedId.Id == id && cachedId.Keys.Any(cachedKey => cachedKey.Key == key));
        }

        return false;
    }

    private class CachedId
    {
        public int Id { get; set; }
        public HashSet<CachedKey> Keys { get; set; } = [];
    }

    private class CachedKey
    {
        public string Key { get; set; }
        public DateTimeOffset Expires { get; set; }
    }
}