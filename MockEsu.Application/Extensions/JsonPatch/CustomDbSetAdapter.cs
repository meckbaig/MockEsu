using Microsoft.AspNetCore.JsonPatch.Internal;
using Microsoft.EntityFrameworkCore;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.Extensions.DataBaseProvider;
using MockEsu.Domain.Common;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Application.Extensions.JsonPatch;

public class CustomDbSetAdapter<TEntity> : IAdapter where TEntity : BaseEntity
{
    public bool TryAdd(
        object target,
        string segment,
        IContractResolver contractResolver,
        object value,
        out string errorMessage)
    {
        throw new NotImplementedException();
    }

    public bool TryGet(
        object target,
        string segment,
        IContractResolver
        contractResolver,
        out object value,
        out string errorMessage)
    {
        throw new NotImplementedException();
    }


    public bool TryRemove(
        object target,
        string segment,
        IContractResolver contractResolver,
        out string errorMessage)
    {
        throw new NotImplementedException();
    }

    public bool TryReplace(
        object target,
        string segmentsString,
        IContractResolver contractResolver,
        object value,
        out string errorMessage)
    {
        string[] segments = segmentsString.Split('.');
        DbSet<TEntity> dbSet = (DbSet<TEntity>)target;
        IQueryable<TEntity> query = dbSet;

        foreach (var segment in segments)
        {
            if (int.TryParse(segment, out int entityId))
            {
                query = query.Where(e => e.Id == entityId);
            }
            else
            {
                Type genericOfSet = query.GetType().GetGenericArguments()[0];
                var prop = genericOfSet.GetProperty(segment);
                if (typeof(IAppDbContext).TryGetDbSetFromInterface(prop.PropertyType.GetGenericArguments()[0], out var newQuery))
                    query = (IQueryable<TEntity>)newQuery;
            }
        }



        //if (!TryGetListTypeArgument(dbSet, out Type typeArgument, out errorMessage))
        //{
        //    return false;
        //}

        throw new NotImplementedException();
    }
    private bool TryReplace<TBaseEntity>(
        IQueryable<TBaseEntity> target,
        string segmentsString,
        IContractResolver contractResolver,
        object value,
        out string errorMessage)
        where TBaseEntity : BaseEntity
    {

        string[] segments = segmentsString.Split('.');
        DbSet<TBaseEntity> dbSet = (DbSet<TBaseEntity>)target;
        IQueryable<TBaseEntity> query = dbSet;

        foreach (var segment in segments)
        {
            if (int.TryParse(segment, out int entityId))
            {
                query = query.Where(e => e.Id == entityId);
            }
            else
            {
                Type genericOfSet = query.GetType().GetGenericArguments()[0];
                var prop = genericOfSet.GetProperty(segment);
                if (typeof(IAppDbContext).TryGetDbSetFromInterface(prop.PropertyType.GetGenericArguments()[0], out var newQuery))
                    query = (IQueryable<TBaseEntity>)newQuery;
            }
        }



        //if (!TryGetListTypeArgument(dbSet, out Type typeArgument, out errorMessage))
        //{
        //    return false;
        //}

        throw new NotImplementedException();
    }

    public bool TryTest(
        object target,
        string segment,
        IContractResolver contractResolver,
        object value,
        out string errorMessage)
    {
        throw new NotImplementedException();
    }

    public bool TryTraverse(
        object target,
        string segment,
        IContractResolver contractResolver,
        out object value,
        out string errorMessage)
    {
        throw new NotImplementedException();
        var entities = (target as IQueryable).Cast<BaseEntity>();
        if (entities == null)
        {
            value = null;
            errorMessage = null;
            return false;
        }

        if (!int.TryParse(segment, out var entityId))
        {
            value = null;
            errorMessage = AdapterError.FormatInvalidIndexValue(segment);
            return false;
        }

        value = string.Empty;
        errorMessage = null;
        return true;
    }
}
