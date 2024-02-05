using Microsoft.AspNetCore.JsonPatch.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using MockEsu.Application.Extensions.DataBaseProvider;
using MockEsu.Domain.Common;
using Newtonsoft.Json.Serialization;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

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

        return TryReplaceWithNewQuery(target, dbSet, segments, contractResolver, value, out errorMessage);




        //if (!TryGetListTypeArgument(dbSet, out Type typeArgument, out errorMessage))
        //{
        //    return false;
        //}

        throw new NotImplementedException();
    }

    private bool TryReplaceWithNewQuery(
        object dbSet,
        IQueryable query,
        Type genericType,
        string[] segments,
        IContractResolver contractResolver,
        object value,
        out string errorMessage)
    {
        var methodInfo = typeof(CustomDbSetAdapter<TEntity>)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(m =>
            m.Name == nameof(TryReplaceWithNewQuery) &&
            m.GetParameters().Length == 6);

        //var methodInfo = typeof(CustomDbSetAdapter<TEntity>)
        //    .GetMethods()
        //    
        var genericMethod = methodInfo.MakeGenericMethod(genericType);
        object[] parameters = [dbSet, query, segments, contractResolver, value, null];
        object result = genericMethod.Invoke(this, parameters);

        errorMessage = (string)parameters.LastOrDefault();
        return (bool)result;
    }

    private bool TryReplaceWithNewQuery<TBaseEntity>(
        object dbSet,
        IQueryable<TBaseEntity> query,
        string[] segments,
        IContractResolver contractResolver,
        object value,
        out string errorMessage)
        where TBaseEntity : BaseEntity
    {
        /// TODO:
        /// проблема с тем, что во вложенности пересоздаётся IQueryable и ничего не работет
        /// проблема в том, что [] всё поломает - решено
        /// проблема в том, что не работает транзакция

        for (int i = 0; i < segments.Length; i++)
        {
            Type? propertyType = typeof(TBaseEntity).GetProperty(segments[i])?.PropertyType;

            if (propertyType == null && int.TryParse(segments[i], out int entityId))
            {
                query = query.Where(e => e.Id == entityId);
            }

            else if (typeof(IEnumerable).IsAssignableFrom(propertyType)
                && TryGetQueryFromSegment(dbSet, query, segments[i], out var newQuery))
            {
                Type entityType = newQuery.ElementType;
                return TryReplaceWithNewQuery(newQuery, newQuery, entityType, segments[++i..], contractResolver, value, out errorMessage);
            }

            else if (propertyType != null &&
                TryConvertValue(value, propertyType!, segments[i], contractResolver, out var convertedValue, out errorMessage))
            {
                //if (!TryGetExecuteUpdateLambda<TBaseEntity>(segments[i], convertedValue, out var expression, out errorMessage))
                //    return false;
                //query.Select(expression);

                TryGetExecuteUpdateLambda<TBaseEntity>(segments[i], convertedValue, out var expression, out errorMessage);
                query.ExecuteUpdate(expression);
            }

            else
            {
                errorMessage = "Can not define expression while executing replace method";
                return false;
            }
        }
        errorMessage = null;
        return true;
    }

    //private static bool TryGetExecuteUpdateLambda<TBaseEntity>(
    //    string propertyName,
    //    object? value,
    //    out Expression<Func<TBaseEntity, object>> expression,
    //    out string errorMessage)
    //{
    //    try
    //    {
    //        var parameter = Expression.Parameter(typeof(TBaseEntity), "x");
    //        var property = Expression.Property(parameter, propertyName);
    //        var constant = Expression.Constant(value, value.GetType());
    //        var assignment = Expression.Assign(property, constant);

    //        expression = Expression.Lambda<Func<TBaseEntity, object>>(assignment, parameter);
    //        errorMessage = null;
    //        return true;
    //    }
    //    catch (Exception ex)
    //    {
    //        expression = null;
    //        errorMessage = ex.Message;
    //        return false;
    //    }
    //}

    private static bool TryGetPropertyLambda<TBaseEntity>(
        string propertyName,
        object? value,
        out Expression<Func<TBaseEntity, object>> expression,
        out string errorMessage)
    {
        try
        {
            var parameter = Expression.Parameter(typeof(TBaseEntity), "x");
            var property = Expression.Property(parameter, propertyName);

            expression = Expression.Lambda<Func<TBaseEntity, object>>(property, parameter);
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            expression = null;
            errorMessage = ex.Message;
            return false;
        }
    }

    private static bool TryGetExecuteUpdateLambda<TBaseEntity>(
        string propertyName,
        object? value,
        out Expression<Func<SetPropertyCalls<TBaseEntity>, SetPropertyCalls<TBaseEntity>>> expression,
        out string errorMessage)
        where TBaseEntity : BaseEntity
    {
        TryGetPropertyLambda<TBaseEntity>(propertyName, value, out var propExpr, out errorMessage);
        var param = Expression.Parameter(typeof(SetPropertyCalls<TBaseEntity>));

        // find method SetProperty(Func<T, TProp>, TProp):
        var method = typeof(SetPropertyCalls<TBaseEntity>)
            .GetMethods()
            .Where(info => info.Name == nameof(SetPropertyCalls<TBaseEntity>.SetProperty))
            // filter out SetProperty(Func<T, TProp>, Func<T, TProp>) overload
            .Where(info => info.GetParameters() is [_, { ParameterType.IsConstructedGenericType: false }])
            .Single();

        // construct appropriately typed generic SetProperty
        var makeGenericMethod = method.MakeGenericMethod(propExpr.Type.GetGenericArguments()[1]);

        var methodCallExpression = Expression.Call(param, makeGenericMethod, propExpr, Expression.Constant(value, value.GetType()));

        // construct final expression
        var lambdaExpression = Expression.Lambda<Func<SetPropertyCalls<TBaseEntity>, SetPropertyCalls<TBaseEntity>>>(methodCallExpression, param);
        lambdaExpression.Compile();
        expression = lambdaExpression;
        return true;
    }

    //protected virtual bool TryGetJsonProperty(
    //    Type targetType,
    //    IContractResolver contractResolver,
    //    string segment,
    //    out JsonProperty jsonProperty)
    //{
    //    if (contractResolver.ResolveContract(targetType) is JsonObjectContract jsonObjectContract)
    //    {
    //        var pocoProperty = jsonObjectContract
    //            .Properties
    //            .FirstOrDefault(p => string.Equals(p.PropertyName, segment, StringComparison.OrdinalIgnoreCase));

    //        if (pocoProperty != null)
    //        {
    //            jsonProperty = pocoProperty;
    //            return true;
    //        }
    //    }

    //    jsonProperty = null;
    //    return false;
    //}

    private static bool TryGetQueryFromSegment<TBaseEntity>(
        object dbSet,
        IQueryable<TBaseEntity> query,
        string segment,
        out IQueryable newQuery)
        where TBaseEntity : BaseEntity
    {
        newQuery = null;
        var propertyInfo = query
            .GetType()
            .GetGenericArguments()[0]
            .GetProperty(segment);
        if (propertyInfo == null || propertyInfo.PropertyType.GetGenericArguments().Length == 0)
            return false;
        Type genericOfSet = propertyInfo.PropertyType.GetGenericArguments()[0];
        return (dbSet as DbSet<TBaseEntity>).TryGetDbSetFromInterface(genericOfSet, out newQuery);

        //var methodInfo = typeof(DataBaseProviderExtensions)
        //    .GetMethods()
        //    .FirstOrDefault(m => 
        //        m.Name == nameof(DataBaseProviderExtensions.TryGetDbSetFromInterface) &&
        //        m.GetParameters().Length == 2);
        //var genericMethod = methodInfo.MakeGenericMethod(genericOfSet);
        //object[] parameters = [typeof(IAppDbContext), null];
        //object result = genericMethod.Invoke(null, parameters);

        //if ((bool)result)
        //    newQuery = (IQueryable)parameters[1];
        //return (bool)result;
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

    protected virtual bool TryConvertValue(
        object originalValue,
        Type listTypeArgument,
        string segment,
        IContractResolver contractResolver,
        out object convertedValue,
        out string errorMessage)
    {
        var conversionResult = ConversionResultProvider.ConvertTo(originalValue, listTypeArgument);
        if (!conversionResult.CanBeConverted)
        {
            convertedValue = null;
            errorMessage = AdapterError.FormatInvalidValueForProperty(originalValue);
            return false;
        }

        convertedValue = conversionResult.ConvertedInstance;
        errorMessage = null;
        return true;
    }
}
