using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Domain.Common;
using MockEsu.Domain.Entities;
using MockEsu.Domain.Entities.Authentification;
using MockEsu.Domain.Entities.Traiffs;
using MockEsu.Infrastructure.Interceptors;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MockEsu.Infrastructure.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    private readonly ILogger<TransactionLoggingInterceptor> _logger;
    private readonly IDistributedCache _cache;

    public AppDbContext(DbContextOptions<AppDbContext> options, ILogger<TransactionLoggingInterceptor> logger, IDistributedCache cache) : base(options)
    {
        _logger = logger;
        _cache = cache;
    }

    public DbSet<Address> Addresses
        => Set<Address>();

    public DbSet<City> Cities
        => Set<City>();

    public DbSet<Kontragent> Kontragents
        => Set<Kontragent>();

    public DbSet<KontragentAgreement> KontragentAgreements
        => Set<KontragentAgreement>();

    public DbSet<Organization> Organizations
        => Set<Organization>();

    public DbSet<Region> Regions
        => Set<Region>();

    public DbSet<Street> Streets
        => Set<Street>();

    public DbSet<PaymentContract> PaymentContracts
        => Set<PaymentContract>();

    public DbSet<User> Users
        => Set<User>();

    public DbSet<Role> Roles
        => Set<Role>();

    public DbSet<PermissionInRole> PermissionsInRoles
        => Set<PermissionInRole>();

    public DbSet<Permission> Permissions
        => Set<Permission>();

    public DbSet<Tariff> Tariffs
        => Set<Tariff>();

    public DbSet<TariffPrice> TariffPrices
        => Set<TariffPrice>();

    public DbSet<OrganizationInRegion> OrganizationsInRegions
        => Set<OrganizationInRegion>();

    public DbSet<RefreshToken> RefreshTokens
        => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        builder.SetDeletedFilters();

        //builder.UseCustomFunctions();

        //builder.Entity<OrganizationInRegion>().HasKey(or => new { or.OrganizationId, or.RegionId });
        //builder.Entity<OrganizationInRegion>()
        //    .HasOne(or => or.Organization)
        //    .WithMany(o => o.OrganizationInRegions)
        //    .HasForeignKey(or => or.OrganizationId);
        //builder.Entity<OrganizationInRegion>()
        //    .HasOne(or => or.Region)
        //    .WithMany(r => r.OrganizationsInRegion)
        //    .HasForeignKey(or => or.RegionId);
        builder.Entity<Organization>()
            .HasMany(o => o.Regions)
            .WithMany(r => r.Organizations)
            .UsingEntity<OrganizationInRegion>();
        builder.Entity<Role>()
            .HasMany(o => o.Permissions)
            .WithMany(r => r.Roles)
            .UsingEntity<PermissionInRole>();

        base.OnModelCreating(builder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new TransactionLoggingInterceptor(_logger, _cache));
        base.OnConfiguring(optionsBuilder);
    }
}


internal static class AppDbContextCustomFunctions
{
    /// <summary>
    /// Migrates all permissions into database.
    /// </summary>
    /// <param name="appDbContext">Data base context.</param>
    internal static void ConfigurePermissions(this IAppDbContext appDbContext)
    {
        HashSet<Permission> localPermissions = Enum.GetValues<Domain.Enums.Permission>()
            .Select(p => new Permission
            {
                Id = (int)p,
                Name = p.ToString()
            })
            .ToHashSet();

        HashSet<Permission> dbPermissions = appDbContext.Permissions.ToHashSet();

        foreach (var localPermission in localPermissions)
        {
            Permission? dbPermission = dbPermissions.FirstOrDefault(p => p.Id == localPermission.Id);
            if (dbPermission == null)
            {
                appDbContext.Permissions.Add(localPermission);
            }
            else
            {
                dbPermission.Name = localPermission.Name;
            }
        }

        HashSet<Permission> removePermissions = dbPermissions.Except(localPermissions, new PermissionsComparer()).ToHashSet();
        foreach (var removePermission in removePermissions)
        {
            appDbContext.Permissions.Remove(removePermission);
        }

        appDbContext.SaveChanges();
    }

    private class PermissionsComparer : IEqualityComparer<Permission>
    {
        public bool Equals(Permission? x, Permission? y)
        {
            return x.Id == y.Id;
        }

        public int GetHashCode([DisallowNull] Permission obj)
        {
            return obj.Id;
        }
    }

    internal static void SetDeletedFilters(this ModelBuilder builder)
    {
        var types = typeof(IAppDbContext)
            .GetProperties().Where(
                p => p.PropertyType.IsGenericType &&
                p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
                typeof(INonDelitableEntity).IsAssignableFrom(
                    p.PropertyType.GetGenericArguments().FirstOrDefault()))
            .Select(p => p.PropertyType.GetGenericArguments().FirstOrDefault())
            .ToList();
        foreach (var type in types)
        {
            var methodInfo = typeof(AppDbContextCustomFunctions)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == nameof(SetDeletedFilter));
            var genericMethod = methodInfo.MakeGenericMethod(type);
            object[] parameters = [builder];
            genericMethod.Invoke(null, parameters);
        }
    }

    private static void SetDeletedFilter<TEntity>(ModelBuilder builder)
        where TEntity : BaseEntity, INonDelitableEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(p => !p.Deleted);
    }

    // TODO: delete
    internal static void UseCustomFunctions(this ModelBuilder modelBuilder) // Does not work
    {
        var methodInfo = typeof(string).GetMethod(
            nameof(String.Equals),
            BindingFlags.Static | BindingFlags.Public,
            null,
            [typeof(string), typeof(string), typeof(StringComparison)],
            null);
        modelBuilder.Model.GetDefaultSchema();
        modelBuilder
            .HasDbFunction(methodInfo)
            .HasTranslation(args =>
            {
                StringComparison sc = (StringComparison)Convert.ToInt32(args[2].Print());
                switch (sc)
                {
                    case StringComparison.CurrentCulture:
                    case StringComparison.InvariantCulture:
                    case StringComparison.Ordinal:
                        return new SqlFunctionExpression(
                            "like",
                            args,
                            false,
                            args.Select(_ => false),
                            typeof(bool),
                            null);
                    case StringComparison.OrdinalIgnoreCase:
                    case StringComparison.InvariantCultureIgnoreCase:
                    case StringComparison.CurrentCultureIgnoreCase:
                        return new SqlFunctionExpression(
                            "ilike",
                            args,
                            false,
                            args.Select(_ => false),
                            typeof(bool),
                            null);
                }
                throw new NotImplementedException();
            });
    }
}