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
    //public static bool TryGetDbSetFromInterface<TEntity>(this Type interfaceType, out IQueryable<TEntity> queryable)
    //{
    //    Type dbSetType = typeof(DbSet<>).MakeGenericType(typeof(TEntity));
    //    bool isDbSet = interfaceType
    //        .GetProperties()
    //        .Any(p => p.PropertyType.IsGenericType &&
    //                  p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
    //                  p.PropertyType == dbSetType);
    //    if (isDbSet)
    //    {
    //        IEnumerable<TEntity> myCollection = (IEnumerable<TEntity>)Array.CreateInstance(typeof(TEntity), 0);
    //        queryable = myCollection.AsQueryable();
    //    }
    //    else
    //    {
    //        queryable = null;
    //    }
    //    return isDbSet;
    //}

    public static bool TryGetDbSetFromInterface<T>(this DbSet<T> dbSet, Type entityType, out IQueryable queryable)
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

    private static IQueryable CreateDbSet(this IAppDbContext context, Type elementType)
    {
        MethodInfo setMethod = typeof(IAppDbContext)
            .GetMethod(nameof(IAppDbContext.Set))
            .MakeGenericMethod(elementType);

        return (IQueryable)setMethod.Invoke(context, null);
    }

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
