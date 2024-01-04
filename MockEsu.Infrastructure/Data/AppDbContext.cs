using Microsoft.EntityFrameworkCore;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Domain.Entities;
using System.Reflection;

namespace MockEsu.Infrastructure.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Address> Addresses => Set<Address>();

    public DbSet<City> Cities => Set<City>();

    public DbSet<Kontragent> Kontragents => Set<Kontragent>();

    public DbSet<KontragentAgreement> KontragentAgreements => Set<KontragentAgreement>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<Region> Regions => Set<Region>();

    public DbSet<Street> Streets => Set<Street>();

    public DbSet<PaymentContract> PaymentContracts => Set<PaymentContract>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(builder);
    }
}
