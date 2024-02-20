using AutoMapper;
using MockEsu.Application.Common.Dtos;
using MockEsu.Application.Common.Exceptions;
using MockEsu.Application.Extensions.JsonPatch;
using MockEsu.Domain.Common;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

namespace MockEsu.Application.Extensions.ListFilters;

/// <summary>
/// Custom EF Core extencion class for dynamic sorting
/// </summary>
public static class EntityFrameworkOrderByExtension
{
    /// <summary>
    /// Adds 'OrderBy' statements using input sort filters and mapping engine
    /// </summary>
    /// <typeparam name="TSource">Source of DTO type</typeparam>
    /// <typeparam name="TDestintaion">DTO type</typeparam>
    /// <param name="source">Queryable source</param>
    /// <param name="orderByExpressions">Array of sort expressions</param>
    /// <returns>An <typeparamref name="IOrderedQueryable"/> that contains sorting</returns>
    public static IOrderedQueryable<TSource> AddOrderBy<TSource, TDestintaion>
        (this IQueryable<TSource> source, List<OrderByExpression>? orderByExpressions)
        where TSource : BaseEntity
        where TDestintaion : BaseDto
    {
        if (orderByExpressions == null)
            return (IOrderedQueryable<TSource>)source;
        IOrderedQueryable<TSource> result = source.OrderBy(x => 0);
        for (int i = 0; i < orderByExpressions.Count; i++)
        {
            result = result.AppendToQuery<TSource, TDestintaion>(orderByExpressions[i]);
        }
        return result;
    }

    private static IOrderedQueryable<TSource> AppendToQuery<TSource, TDestintaion>
        (this IOrderedQueryable<TSource> source, OrderByExpression orderByEx)
        where TSource : BaseEntity
        where TDestintaion : BaseDto
    {
        var param = Expression.Parameter(typeof(TSource), "x");

        string[] endpoint = orderByEx.EndPoint.Split('.');
        MemberExpression propExpression = Expression.Property(param, endpoint[0]);
        if (endpoint.Length != 1)
        {
            for (int i = 1; i < endpoint.Length; i++)
            {
                propExpression = Expression.Property(propExpression, endpoint[i]);
            }
        }

        var func = typeof(Func<,>);
        var genericFunc = func.MakeGenericType(typeof(TSource), propExpression.Type);
        var lambda = Expression.Lambda(genericFunc, propExpression, param);

        switch (orderByEx.ExpressionType)
        {
            case OrderByExpressionType.Ascending:
                return source.ThenBy($"{lambda}");
            case OrderByExpressionType.Descending:
                return source.ThenBy($"{lambda} desc");
            default:
                return source;
        }
    }

    /// <summary>
    /// Gets full endpoint route string
    /// </summary>
    /// <typeparam name="TSource">Source of DTO type</typeparam>
    /// <typeparam name="TDestintaion">DTO type</typeparam>
    /// <param name="destinationPropertyName">Source prioer</param>
    /// <param name="provider">Configuraion provider for performing maps</param>
    /// <returns>Returns endpoint if success, null if error</returns>
    public static string GetExpressionEndpoint<TSource, TDestintaion>
        (string destinationPropertyName, IConfigurationProvider provider)
    {
        return DtoExtension.GetSource<TSource, TDestintaion>
            (destinationPropertyName, provider, throwException: false);
    }

    /// <summary>
    /// Gets sorting expression
    /// </summary>
    /// <typeparam name="TSource">Source of DTO type</typeparam>
    /// <typeparam name="TDestintaion">DTO type</typeparam>
    /// <param name="sortingExpressionString">Sorting expression from client</param>
    /// <param name="provider">Configuraion provider for performing maps</param>
    /// <returns>Returns OrderByExpression model if success, undefined OrderByExpression
    /// if can not parse expression, null if error</returns>
    internal static OrderByExpression GetOrderByExpression
        <TSource, TDestintaion>
        (string sortingExpressionString, IConfigurationProvider provider)
        where TSource : BaseEntity
        where TDestintaion : BaseDto
    {
        try
        {
            return OrderByExpression.Initialize<TSource, TDestintaion>
                (sortingExpressionString, provider);
        }
        catch (ValidationException)
        {
            return null;
        }
    }
}
