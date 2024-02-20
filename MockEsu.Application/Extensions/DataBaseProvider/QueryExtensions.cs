using Microsoft.EntityFrameworkCore;
using MockEsu.Domain.Entities;
using MockEsu.Domain.Entities.Authentification;
using MockEsu.Domain.Entities.Traiffs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Application.Extensions.DataBaseProvider;

public static class QueryExtensions
{
    /// <returns>Tariffs and their prices</returns>
    public static IQueryable<Tariff> WithPrices(this IQueryable<Tariff> tariffs)
    {
        return tariffs.Include(t => t.Prices.OrderBy(p => p.Id));
    }

    public static IQueryable<Kontragent> FullData(this IQueryable<Kontragent> kontragents)
    {
        return kontragents
            .Include(k => k.KontragentAgreement)
            .Include(k => k.Address).ThenInclude(a => a.City)
            .Include(k => k.Address).ThenInclude(a => a.Street)
            .Include(k => k.Address).ThenInclude(a => a.Region);
    }

    /// <returns>User with role (including permiissions)</returns>
    public static User WithRoleById(this IQueryable<User> users, int id)
    {
        return users.Include(u => u.Role).ThenInclude(r => r.Permissions)
            .FirstOrDefault(k => k.Id == id);
    }
}
