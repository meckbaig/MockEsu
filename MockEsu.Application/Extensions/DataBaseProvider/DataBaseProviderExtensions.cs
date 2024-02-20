using Microsoft.AspNetCore.JsonPatch.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MockEsu.Application.Common.Dtos;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.Extensions.JsonPatch;
using MockEsu.Domain.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Application.Extensions.DataBaseProvider;

internal static class DataBaseProviderExtensions
{
    /// <summary>
    /// Gets DbSet with selected  type from DbContext in the DbSet that called the method.
    /// </summary>
    /// <typeparam name="T">Type of generic in the DbSet.</typeparam>
    /// <param name="dbSet">The DbSet from which the context will be taken</param>
    /// <param name="entityType">Type of generic in the result DbSet.</param>
    /// <param name="queryable">Result DbSet.</param>
    /// <returns><see langword="true"/> if <paramref name="queryable" /> was found successfully; otherwise, false.</returns>
    public static bool TryGetDbSetFromAnotherDbSet<T>(this DbSet<T> dbSet, Type entityType, out IQueryable queryable)
        where T : class
    {
        Type dbSetType = typeof(DbSet<>).MakeGenericType(entityType);
        bool isDbSet = typeof(IAppDbContext)
            .GetProperties()
            .Any(p => p.PropertyType.IsGenericType &&
                      p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
                      p.PropertyType == dbSetType);
        if (isDbSet)
        {
            var context = dbSet.GetService<IAppDbContext>();
            queryable = context.CreateDbSet(entityType);
        }
        else
        {
            queryable = null;
        }
        return isDbSet;
    }

    internal static IQueryable CreateDbSet(this IAppDbContext context, Type elementType)
    {
        MethodInfo setMethod = typeof(IAppDbContext)
            .GetMethod(nameof(IAppDbContext.Set))
            .MakeGenericMethod(elementType);

        return (IQueryable)setMethod.Invoke(context, null);
    }

    /// <summary>
    /// Possibility of getting DbSet with selected type.
    /// </summary>
    /// <param name="interfaceType">DbContext interface.</param>
    /// <param name="entityType">Type of generic in the result DbSet.</param>
    /// <returns><see langword="true"/> if DbSet was found; otherwise, false.</returns>
    public static bool CanGetDbSet(this Type interfaceType, Type entityType)
    {
        Type dbSetType = typeof(DbSet<>).MakeGenericType(entityType);
        return interfaceType
            .GetProperties()
            .Any(p => p.PropertyType.IsGenericType &&
                      p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
                      p.PropertyType == dbSetType);
    }
}
