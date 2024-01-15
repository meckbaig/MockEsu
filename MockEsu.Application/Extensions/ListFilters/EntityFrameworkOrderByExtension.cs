using MockEsu.Application.Common;
using MockEsu.Domain.Common;
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
        for (int i = 0; i < orderByExpressions.Count; i++)
        {
            source = source.AppendToQuery<TSource, TDestintaion>(orderByExpressions[i], i > 0);
        }
        return (IOrderedQueryable<TSource>)source;
    }

    private static IOrderedQueryable<TSource> AppendToQuery<TSource, TDestintaion>
        (this IQueryable<TSource> source, OrderByExpression orderByEx, bool thenBy)
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

        Expression<Func<TSource, object>> filterLambda
            = Expression.Lambda<Func<TSource, object>>(propExpression, param);

        if (thenBy)
            switch (orderByEx.ExpressionType)
            {
                case OrderByExpressionType.Ascending:
                    (source as IOrderedQueryable<TSource>).ThenBy(filterLambda);
                    break;
                case OrderByExpressionType.Descending:
                    (source as IOrderedQueryable<TSource>).ThenByDescending(filterLambda);
                    break;
                default:
                    break;
            }
        else
            switch (orderByEx.ExpressionType)
            {
                case OrderByExpressionType.Ascending:
                    source.OrderBy(filterLambda);
                    break;
                case OrderByExpressionType.Descending:
                    source.OrderByDescending(filterLambda);
                    break;
                default:
                    break;
            }
        return (IOrderedQueryable<TSource>)source;
    }
}
