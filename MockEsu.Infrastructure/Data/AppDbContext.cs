using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.Logging;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Domain.Common;
using MockEsu.Domain.Entities;
using MockEsu.Domain.Entities.Traiffs;
using MockEsu.Infrastructure.Interceptors;
using System.Reflection;

namespace MockEsu.Infrastructure.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    private readonly ILogger<TransactionLoggingInterceptor> _logger;

    public AppDbContext(DbContextOptions<AppDbContext> options, ILogger<TransactionLoggingInterceptor> logger) : base(options)
    {
        _logger = logger;
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

    public IQueryable<User> UsersInServiceQuery
        => Users.Where(u => !u.Deleted);

    public DbSet<Role> Roles
        => Set<Role>();

    public DbSet<Tariff> Tariffs
        => Set<Tariff>();

    public DbSet<TariffPrice> TariffPrices
        => Set<TariffPrice>();

    public DbSet<OrganizationInRegion> OrganizationsInRegions
        => Set<OrganizationInRegion>();

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

        base.OnModelCreating(builder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new TransactionLoggingInterceptor(_logger));
        base.OnConfiguring(optionsBuilder);
    }
}


internal static class AppDbContextCustomFunctions
{
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
            var methodInfo = typeof(AppDbContext)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static).FirstOrDefault(m =>
                m.Name == nameof(SetDeletedFilter));
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