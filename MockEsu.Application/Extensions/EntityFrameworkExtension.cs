using AutoMapper;
using MockEsu.Application.Common;
using MockEsu.Application.Common.Attributes;
using MockEsu.Application.DTOs.Kontragents;
using MockEsu.Domain.Common;
using MockEsu.Domain.Entities;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;

namespace MockEsu.Infrastructure.Extensions;

/// <summary>
/// Custom EF Core extencion class for dynamic filtering
/// </summary>
public static class EntityFrameworkExtension
{
    /// <summary>
    /// Adds "Where" statements using input filters and mapping engine
    /// </summary>
    /// <typeparam name="TSource">Source of DTO type</typeparam>
    /// <typeparam name="TDestintaion">DTO type</typeparam>
    /// <param name="source">Queryable source</param>
    /// <param name="provider">Configuraion provider for performing maps</param>
    /// <param name="filters">Array of filters</param>
    /// <returns>An <typeparamref name="IQueryable"/> that contains filters</returns>
    public static IQueryable<TSource> AddFilters<TSource, TDestintaion>
        (this IQueryable<TSource> source, IConfigurationProvider provider, string[]? filters)
        where TSource : BaseEntity
        where TDestintaion : BaseDto
    {
        if (filters == null)
            return source;
        foreach (var filter in filters)
        {
            source = source.AddFilter<TSource, TDestintaion>(provider, filter);
        }
        return source;
    }

    /// <summary>
    /// Adds "Where" statements using input filter and mapping engine
    /// </summary>
    /// <typeparam name="TSource">Source of DTO type</typeparam>
    /// <typeparam name="TDestintaion">DTO type</typeparam>
    /// <param name="source">Queryable source</param>
    /// <param name="provider">Configuraion provider for performing maps</param>
    /// <param name="filter">Filter string</param>
    /// <returns>An <typeparamref name="IQueryable"/> that contains filter</returns>
    private static IQueryable<TSource> AddFilter<TSource, TDestintaion>
        (this IQueryable<TSource> source, IConfigurationProvider provider, string filter)
        where TSource : BaseEntity
        where TDestintaion : BaseDto
    {
        var filterEx = new FilterExpression<TSource, TDestintaion>(filter, provider);
        if (filterEx.ExpressionType == ExpressionType.Undefined)
            return source;

        PropertyInfo prop = typeof(TDestintaion).GetProperties()
            .FirstOrDefault(p => p.Name == filterEx.Key)!;
        if (prop == null)
            return source;

        FilterableAttribute attribute = (FilterableAttribute)prop.GetCustomAttributes(true)
            .FirstOrDefault(a => a.GetType() == typeof(FilterableAttribute))!;
        if (attribute == null)
            return source;

        return AppendToQuery(source, attribute.CompareMethod, filterEx);
    }

    /// <summary>
    /// Applies filter expression to source
    /// </summary>
    /// <typeparam name="TSource">Source of DTO type</typeparam>
    /// <typeparam name="TDestintaion">DTO type</typeparam>
    /// <param name="source">Queryable source</param>
    /// <param name="compareMethod">Сomparison method</param>
    /// <param name="filterEx">Expression to apply to source</param>
    /// <returns>An <typeparamref name="IQueryable"/> that contains filter</returns>
    private static IQueryable<TSource> AppendToQuery<TSource, TDestintaion>
        (IQueryable<TSource> source, CompareMethod compareMethod, FilterExpression<TSource, TDestintaion> filterEx)
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
        Expression expression;
        switch (compareMethod)
        {
            case CompareMethod.Equals:
                expression = EqualExpression(values, propExpression, filterEx.ExpressionType);
                break;
            case CompareMethod.ById:
                expression = ByIdExpression3(values, propExpression, filterEx.ExpressionType);
                break;
            default:
                return source;
        }

        Expression<Func<TSource, bool>> filterLambda 
            = Expression.Lambda<Func<TSource, bool>>(expression, param);

        return source.Where(filterLambda);
    }

    /// <summary>
    /// Creates Equal() lambda expression from array of filter strings
    /// </summary>
    /// <param name="values">Filter strings</param>
    /// <param name="propExpression">A field of property</param>
    /// <param name="expressionType">Type of expression</param>
    /// <returns>Lambda expression with Equal() filter</returns>
    private static Expression EqualExpression(object[] values, MemberExpression propExpression, ExpressionType expressionType)
    {
        if (values.Length == 0)
            return Expression.Empty();
        Expression expression = Expression.Empty();
        for (int i = 0; i < values.Length; i++)
        {
            Expression newExpression;
            if (values[i].ToString()!.Contains(".."))
            {
                string valueString = values[i].ToString();
                object from = ConvertFromObject(valueString.Substring(0, valueString.IndexOf("..")), propExpression.Type);
                object to = ConvertFromObject(valueString.Substring(valueString.IndexOf("..") + 2), propExpression.Type);
                newExpression = Expression.AndAlso(
                        Expression.GreaterThanOrEqual(propExpression, Expression.Constant(from, propExpression.Type)),
                        Expression.LessThanOrEqual(propExpression, Expression.Constant(to, propExpression.Type))
                    );
            }
            else
            {
                object value = ConvertFromObject(values[i], propExpression.Type);
                newExpression = Expression.Equal(propExpression, Expression.Constant(value, propExpression.Type));
            }

            if (i == 0)
                expression = newExpression;
            else
                expression = Expression.OrElse(expression, newExpression);
        }
        if (expressionType == ExpressionType.Include)
            return expression;
        else
            return Expression.Not(expression);
    }

    // Делает лишний join, не обязательна конвенция
    /// <summary>
    /// Creates Equal() lambda expression by id from array of filter strings
    /// </summary>
    /// <param name="values">Filter strings</param>
    /// <param name="propExpression">A field of property</param>
    /// <param name="expressionType">Type of expression</param>
    /// <returns>Lambda expression with Equal() filter by id</returns>
    private static Expression ByIdExpression(object[] values, MemberExpression propExpression, ExpressionType expressionType)
    {
        propExpression = Expression.Property(propExpression, "Id");
        return EqualExpression(values, propExpression, expressionType);
    }

    // Попытка убрать лишний join, нужно соблюдение код конвенции
    /// <summary>
    /// Creates Equal() lambda expression by id from array of filter strings
    /// </summary>
    /// <param name="values">Filter strings</param>
    /// <param name="propExpression">A field of property</param>
    /// <param name="expressionType">Type of expression</param>
    /// <returns>Lambda expression with Equal() filter by id</returns>
    private static Expression ByIdExpression2(object[] values, MemberExpression propExpression, ExpressionType expressionType)
    {
        propExpression = Expression.Property(propExpression.Expression, $"{propExpression.Member.Name}Id");
        return EqualExpression(values, propExpression, expressionType);
    }

    // Попытка убрать лишний join, нужно соблюдение указания foreign key
    /// <summary>
    /// Creates Equal() lambda expression by id from array of filter strings
    /// </summary>
    /// <param name="values">Filter strings</param>
    /// <param name="propExpression">A field of property</param>
    /// <param name="expressionType">Type of expression</param>
    /// <returns>Lambda expression with Equal() filter by id</returns>
    private static Expression ByIdExpression3(object[] values, MemberExpression propExpression, ExpressionType expressionType)
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

    /// <summary>
    /// Class representing a filtering expression
    /// </summary>
    /// <typeparam name="TSource">Source of DTO type</typeparam>
    /// <typeparam name="TDestintaion">DTO type</typeparam>
    private record FilterExpression<TSource, TDestintaion>
        where TSource : BaseEntity
        where TDestintaion : BaseDto
    {
        /// <summary>
        /// DTO key
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// Source endpoint key
        /// </summary>
        public string? EndPoint { get; set; }

        /// <summary>
        /// Type of expression
        /// </summary>
        public ExpressionType ExpressionType { get; set; }

        /// <summary>
        /// Filter value
        /// </summary>
        public string? Value { get; set; }

        public FilterExpression(string filter, IConfigurationProvider provider)
        {
            if (filter.Contains("!:"))
            {
                Key = ToPascalCase(filter.Substring(0, filter.IndexOf("!:")));
                EndPoint = BaseDto.GetSource<TSource, TDestintaion>(provider, Key);
                Value = filter.Substring(filter.IndexOf("!:") + 2);
                ExpressionType = ExpressionType.Exclude;
            }
            else if (filter.Contains(":"))
            {
                Key = ToPascalCase(filter.Substring(0, filter.IndexOf(":")));
                EndPoint = BaseDto.GetSource<TSource, TDestintaion>(provider, Key);
                Value = filter.Substring(filter.IndexOf(":") + 1);
                ExpressionType = ExpressionType.Include;
            }
            else
                ExpressionType = ExpressionType.Undefined;
        }

        /// <summary>
        /// Converts a string to pascal case
        /// </summary>
        /// <param name="value">input string</param>
        /// <returns>String in pascal case</returns>
        private string ToPascalCase(string value)
        {
            if (value.Length <= 1)
                return value.ToUpper();
            return $"{value[0].ToString().ToUpper()}{value.Substring(1)}";
        }
    }

    public static object ConvertFromString(this string value, Type type)
    {
        if (type == typeof(DateOnly) || type == typeof(DateOnly?))
            return DateOnly.Parse(value);
        return Convert.ChangeType(value, type);
    }

    public static object ConvertFromObject(object value, Type type)
        => value.ToString().ConvertFromString(type);

    private enum ExpressionType
    {
        Include, Exclude, Undefined
    }
}