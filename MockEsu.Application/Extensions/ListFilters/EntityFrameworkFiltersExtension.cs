using AutoMapper;
using MockEsu.Application.Common;
using MockEsu.Application.Common.Attributes;
using MockEsu.Application.Common.Exceptions;
using MockEsu.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;

namespace MockEsu.Application.Extensions.ListFilters;

/// <summary>
/// Custom EF Core extencion class for dynamic filtering
/// </summary>
public static class EntityFrameworkFiltersExtension
{
    /// <summary>
    /// Adds 'Where' statements using input filters and mapping engine
    /// </summary>
    /// <typeparam name="TSource">Source of DTO type</typeparam>
    /// <typeparam name="TDestintaion">DTO type</typeparam>
    /// <param name="source">Queryable source</param>
    /// <param name="filterExpressions">Array of filter expressions</param>
    /// <returns>An <typeparamref name="IQueryable"/> that contains filters</returns>
    public static IQueryable<TSource> AddFilters<TSource, TDestintaion>
        (this IQueryable<TSource> source, List<Expression>? filterExpressions)
        where TSource : BaseEntity
        where TDestintaion : BaseDto
    {
        if (filterExpressions == null)
            return source;
        foreach (var expression in filterExpressions)
        {
            source = source.AppendToQuery<TSource, TDestintaion>(expression);
        }
        return source;
    }

    private static IQueryable<TSource> AppendToQuery<TSource, TDestintaion>
        (this IQueryable<TSource> source, Expression expression)
        where TSource : BaseEntity
        where TDestintaion : BaseDto
    {
        var param = Expression.Parameter(typeof(TSource), "x");
        Expression<Func<TSource, bool>> filterLambda
            = Expression.Lambda<Func<TSource, bool>>(expression, param);

        return source.Where(filterLambda.ToString());
    }

    /// <summary>
    /// Gets full endpoint route string
    /// </summary>
    /// <typeparam name="TSource">Source of DTO type</typeparam>
    /// <typeparam name="TDestintaion">DTO type</typeparam>
    /// <param name="destinationPropertyName">Source prioer</param>
    /// <param name="provider">Configuraion provider for performing maps</param>
    /// <returns>Returns endpoint if success, null if error</returns>
    public static string? GetExpressionEndpoint<TSource, TDestintaion>
        (string destinationPropertyName, IConfigurationProvider provider)
    {
        return BaseDto.GetSource<TSource, TDestintaion>
            (destinationPropertyName, provider, throwException: false);
    }

    /// <summary>
    /// Gets filter expression
    /// </summary>
    /// <typeparam name="TSource">Source of DTO type</typeparam>
    /// <typeparam name="TDestintaion">DTO type</typeparam>
    /// <param name="filter">Filter from client</param>
    /// <param name="provider">Configuraion provider for performing maps</param>
    /// <returns>Returns FilterExpression model if success, undefined FilterExpression
    /// if can not parse expression, null if error</returns>
    public static FilterExpression? GetFilterExpression
        <TSource, TDestintaion>
        (string filter, IConfigurationProvider provider)
        where TSource : BaseEntity
        where TDestintaion : BaseDto
    {
        try
        {
            return FilterExpression.Initialize<TSource, TDestintaion>(filter, provider);
        }
        catch (ValidationException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets filter attribute
    /// </summary>
    /// <typeparam name="TSource">Source of DTO type</typeparam>
    /// <typeparam name="TDestintaion">DTO type</typeparam>
    /// <param name="filterEx">Filter expression to get the key</param>
    /// <returns>Returns FilterableAttribute model if success, null if error</returns>
    public static FilterableAttribute GetFilterAttribute<TSource, TDestintaion>
        (FilterExpression filterEx)
        where TSource : BaseEntity
        where TDestintaion : BaseDto
    {
        var prop = typeof(TDestintaion).GetProperties()
                .FirstOrDefault(p => p.Name == filterEx.Key)!;
        var attribute = (FilterableAttribute)prop.GetCustomAttributes(true)
            .FirstOrDefault(a => a.GetType() == typeof(FilterableAttribute))!;
        return attribute;
    }

    /// <summary>
    /// Gets filter expression for Where statement
    /// </summary>
    /// <typeparam name="TSource">Source of DTO type</typeparam>
    /// <typeparam name="TDestintaion">DTO type</typeparam>
    /// <param name="compareMethod">Method of comparison</param>
    /// <param name="filterEx">Filter expression</param>
    /// <returns>Filter expression if success, null if error</returns>
    public static Expression? GetLinqExpression<TSource, TDestintaion>
        (CompareMethod compareMethod, FilterExpression filterEx)
        where TSource : BaseEntity
        where TDestintaion : BaseDto
    {
        var param = Expression.Parameter(typeof(TSource), "x");

        string[] endpoint = filterEx.EndPoint.Split('.');
        MemberExpression propExpression = Expression.Property(param, endpoint[0]);
        if (endpoint.Length != 1)
        {
            for (int i = 1; i < endpoint.Length; i++)
            {
                propExpression = Expression.Property(propExpression, endpoint[i]);
            }
        }

        object[] values = filterEx.Value.Split(',');
        switch (compareMethod)
        {
            case CompareMethod.Equals:
                return EqualExpression(values, propExpression, filterEx.ExpressionType);
            case CompareMethod.ById:
                return ByIdExpression(values, propExpression, filterEx.ExpressionType);
            default:
                return null;
        }
    }

    /// <summary>
    /// Creates Equal() lambda expression from array of filter strings
    /// </summary>
    /// <param name="values">Filter strings</param>
    /// <param name="propExpression">A field of property</param>
    /// <param name="expressionType">Type of expression</param>
    /// <returns>Lambda expression with Equal() filter</returns>
    private static Expression EqualExpression
        (object[] values, MemberExpression propExpression, FilterExpressionType expressionType)
    {
        if (values.Length == 0)
            return Expression.Empty();
        Expression expression = Expression.Empty();
        for (int i = 0; i < values.Length; i++)
        {
            if (i == 0)
                expression = GetSingleEqualExpression(values[i], propExpression);
            else
                expression = Expression.OrElse(expression,
                    GetSingleEqualExpression(values[i], propExpression));
        }
        if (expressionType == FilterExpressionType.Include)
            return expression;
        else
            return Expression.Not(expression);
    }

    private static Expression GetSingleEqualExpression(object value, MemberExpression propExpression)
    {
        if (value.ToString()!.Contains(".."))
        {
            string valueString = value.ToString();
            object from = ConvertFromObject(
                valueString.Substring(0, valueString.IndexOf("..")), propExpression.Type);
            object to = ConvertFromObject(
                valueString.Substring(valueString.IndexOf("..") + 2), propExpression.Type);
            List<BinaryExpression> binaryExpressions = new List<BinaryExpression>();
            if (from != null)
                binaryExpressions.Add(Expression.GreaterThanOrEqual(
                    propExpression, Expression.Constant(from, propExpression.Type)));
            if (to != null)
                binaryExpressions.Add(Expression.LessThanOrEqual(
                    propExpression, Expression.Constant(to, propExpression.Type)));
            switch (binaryExpressions.Count)
            {
                case 2:
                    return Expression.AndAlso(binaryExpressions[0], binaryExpressions[1]);
                case 1:
                    return binaryExpressions[0];
                default:
                    throw new Exception($"Can not translate expression {valueString}");
            }
        }
        else
        {
            return Expression.Equal(
                propExpression,
                Expression.Constant(
                    ConvertFromObject(value, propExpression.Type),
                    propExpression.Type));
        }
    }

    /// <summary>
    /// Creates Equal() lambda expression by id from array of filter strings
    /// </summary>
    /// <param name="values">Filter strings</param>
    /// <param name="propExpression">A field of property</param>
    /// <param name="expressionType">Type of expression</param>
    /// <returns>Lambda expression with Equal() filter by id</returns>
    private static Expression ByIdExpression
        (object[] values, MemberExpression propExpression, FilterExpressionType expressionType)
    {
        propExpression = Expression.Property(
            propExpression.Expression,
            GetForeignKeyFromModel(propExpression.Expression.Type, propExpression.Member.Name));
        return EqualExpression(values, propExpression, expressionType);
    }

    private static string GetForeignKeyFromModel(Type type, string modelName)
    {
        PropertyInfo prop = type.GetProperties().FirstOrDefault(
            p => ((ForeignKeyAttribute)p.GetCustomAttributes(true).FirstOrDefault(
                a => a.GetType() == typeof(ForeignKeyAttribute)))?.Name == modelName)!;
        return prop != null ? prop.Name : string.Empty;
    }

    private static object ConvertFromString(this string value, Type type)
    {
        if (value == "")
            return null;
        if (type == typeof(DateOnly) || type == typeof(DateOnly?))
            return DateOnly.Parse(value);
        return Convert.ChangeType(value, type);
    }

    private static object ConvertFromObject(object value, Type type)
        => value.ToString().ConvertFromString(type);
}