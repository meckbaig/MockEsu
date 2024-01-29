using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Domain.Entities;
using System.Reflection;

namespace MockEsu.Infrastructure.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        //builder.UseCustomFunctions();

        base.OnModelCreating(builder);
    }
}

internal static class AppDbContextCustomFunctions
{
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