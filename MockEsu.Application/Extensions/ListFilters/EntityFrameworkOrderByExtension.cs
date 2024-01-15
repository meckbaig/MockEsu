using AutoMapper;
using MockEsu.Application.Common;
using MockEsu.Application.Common.Exceptions;
using MockEsu.Domain.Common;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

namespace MockEsu.Application.Extensions.ListFilters;

public static class EntityFrameworkOrderByExtension
{
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
            result = result.AppendToQuery<TSource, TDestintaion>(orderByExpressions[i], i > 0);
        }
        return result;
    }

    public static string GetExpressionEndpoint<TSource, TDestintaion>(string sourceProperty, IConfigurationProvider provider)
    {
        return BaseDto.GetSource<TSource, TDestintaion>(sourceProperty, provider, throwException: false);
    }

    private static IOrderedQueryable<TSource> AppendToQuery<TSource, TDestintaion>
        (this IOrderedQueryable<TSource> source, OrderByExpression orderByEx, bool thenBy)
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
                return source.ThenBy(lambda.ToString());
            case OrderByExpressionType.Descending:
                return source.ThenBy($"{lambda} desc");
            default:
                return source;
        }
    }

    internal static OrderByExpression GetOrderByExpression<TSource, TDestintaion>(string filter, IConfigurationProvider provider)
        where TSource : BaseEntity
        where TDestintaion : BaseDto
    {
        try
        {
            return OrderByExpression.Initialize<TSource, TDestintaion>(filter, provider);
        }
        catch (ValidationException)
        {
            return null;
        }
    }
}
