using Microsoft.EntityFrameworkCore;
using MockEsu.Application.Common.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;
using static MockEsu.Infrastructure.Caching.SqlCachedKeysProvider;

namespace MockEsu.Infrastructure.Caching;

internal class SqlCachedKeysProvider : ICachedKeysProvider
{
    private readonly ICachedKeysContext _context;
    private readonly List<CachedKey> _cachedKeys;

    public SqlCachedKeysProvider(ICachedKeysContext context)
    {
        _context = context;
        _cachedKeys = [];
    }

    public async Task<bool> TryAddKeyToIdIfNotPresentAsync(string key, DateTimeOffset expires, Type entityType, int id)
    {
        var cachedKey = await GetCachedKeyAsync(key, expires);
        if (!cachedKey.TypeIdPairs.Any(p => p.EntityId == id && p.Type == entityType.Name))
        {
            cachedKey.TypeIdPairs.Add(new TypeIdPair(id, entityType.Name));
            return true;
        }
        return false;
    }

    public async Task<bool> TryCompleteFormationAsync(string key)
    {
        await _context.SaveChangesAsync();
        int rows = await _context.CachedKeys
            .Where(k => k.Key == key)
            .ExecuteUpdateAsync(k => k.SetProperty(k => k.FormationCompleted, true));
        return rows > 0;
    }

    public async Task<List<string>> GetAndRemoveKeysByIdAsync(Type entityType, int id)
    {
        await ClearExpiredKeys();

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

    private async Task<CachedKey> GetCachedKeyAsync(string key, DateTimeOffset expires)
    {
        CachedKey cachedKey = _cachedKeys.FirstOrDefault(k => k.Key == key);
        cachedKey ??= await _context.CachedKeys.Include(k => k.TypeIdPairs).FirstOrDefaultAsync(k => k.Key == key);
        if (cachedKey == null)
        {
            cachedKey = new CachedKey(key, expires);
            await _context.CachedKeys.AddAsync(cachedKey);
        }
        _cachedKeys.Add(cachedKey);
        return cachedKey;
    }

    internal class CachedKey
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public DateTimeOffset Expires { get; set; }
        public HashSet<TypeIdPair> TypeIdPairs { get; set; } = [];
        public bool FormationCompleted { get; set; } = false;

        public CachedKey() { }

        public CachedKey(string key, DateTimeOffset expires)
        {
            Key = key;
            Expires = expires;
        }
    }

    internal class TypeIdPair
    {
        public int EntityId { get; set; }
        public string Type { get; set; }
        [ForeignKey(nameof(CachedKey))]
        public int CachedKeyId { get; set; }
        public CachedKey CachedKey { get; set; }

        public TypeIdPair() { }

        public TypeIdPair(int entityId, string type, int cachedKeyId = 0)
        {
            EntityId = entityId;
            Type = type;
            CachedKeyId = cachedKeyId;
        }
    }
}

internal interface ICachedKeysContext
{
    DbSet<CachedKey> CachedKeys { get; }
    DbSet<TypeIdPair> TypeIdPairs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
}

internal class CachedKeysContext : DbContext, ICachedKeysContext
{
    public CachedKeysContext(DbContextOptions<CachedKeysContext> options) : base(options)
    {
    }

    public DbSet<CachedKey> CachedKeys
        => Set<CachedKey>();

    public DbSet<TypeIdPair> TypeIdPairs
        => Set<TypeIdPair>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TypeIdPair>()
            .HasKey(t => new { t.CachedKeyId, t.Type, t.EntityId });

        //modelBuilder.Entity<CachedKey>()
        //    .HasMany(c => c.TypeIdPairs)
        //    .WithOne(t => t.CachedKey)
        //    .HasForeignKey(t => t.CachedKeyId);

        modelBuilder.Entity<TypeIdPair>()
            .HasOne(t => t.CachedKey)
            .WithMany(c => c.TypeIdPairs);


        base.OnModelCreating(modelBuilder);
    }
}