using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MockEsu.Domain.Entities;
using MockEsu.Domain.Entities.Authentification;
using MockEsu.Domain.Entities.Traiffs;

namespace MockEsu.Application.Common.Interfaces;

public interface IAppDbContext : IDbContext
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
    DbSet<Role> Roles { get; }
    DbSet<PermissionInRole> PermissionsInRoles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<Tariff> Tariffs { get; }
    DbSet<TariffPrice> TariffPrices { get; }
    DbSet<OrganizationInRegion> OrganizationsInRegions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
}
