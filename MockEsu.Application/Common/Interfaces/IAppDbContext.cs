using Microsoft.EntityFrameworkCore;
using MockEsu.Domain.Entities;
using MockEsu.Domain.Entities.Traiffs;

namespace MockEsu.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Address> Addresses { get; }
    DbSet<City> Cities { get; }
    DbSet<Kontragent> Kontragents { get; }
    DbSet<KontragentAgreement> KontragentAgreements { get; }
    DbSet<Organization> Organizations { get; }
    DbSet<Region> Regions { get; }
    DbSet<Street> Streets { get; }
    DbSet<PaymentContract> PaymentContracts { get; }
    DbSet<User> Users { get; }
    IQueryable<User> UsersInService { get; }
    DbSet<Role> Roles { get; }
    DbSet<Tariff> Tariffs { get; }
    DbSet<TariffPrice> TariffPrices { get; }


    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
}
