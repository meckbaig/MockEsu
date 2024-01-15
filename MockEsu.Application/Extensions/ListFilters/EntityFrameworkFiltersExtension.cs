using AutoMapper;
using MockEsu.Application.Common;
using MockEsu.Application.Common.Attributes;
using MockEsu.Application.Common.Exceptions;
using MockEsu.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace MockEsu.Application.Extensions.JournalFilters;

/// <summary>
/// Custom EF Core extencion class for dynamic filtering
/// </summary>
public static class EntityFrameworkFiltersExtension
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
        (this IQueryable<TSource> source, Dictionary<FilterExpression, FilterableAttribute>? filterExpressions)
        where TSource : BaseEntity
        where TDestintaion : BaseDto
    {
        if (filterExpressions == null)
            return source;
        foreach (var expression in filterExpressions)
        {
            source = source.AppendToQuery<TSource, TDestintaion>(expression.Value.CompareMethod, expression.Key);
        }
        return source;
    }

    public static (FilterExpression, FilterableAttribute) ParseFilterToExpression<TSource, TDestintaion>
        (IConfigurationProvider provider, string filter, ref string message, ref ValidationErrorCode code)
        where TSource : BaseEntity
        where TDestintaion : BaseDto
    {
        try
        {
            var filterEx = FilterExpression.Initialize<TSource, TDestintaion>(filter, provider);
            if (filterEx.ExpressionType == FilterExpressionType.Undefined)
            {
                message = $"{filter} - expression is undefined";
                code = ValidationErrorCode.ExpressionIsUndefined;
                return (null, null);
            }

            PropertyInfo prop = typeof(TDestintaion).GetProperties()
                .FirstOrDefault(p => p.Name == filterEx.Key)!;

            FilterableAttribute attribute = (FilterableAttribute)prop.GetCustomAttributes(true)
                .FirstOrDefault(a => a.GetType() == typeof(FilterableAttribute))!;
            if (attribute == null)
            {
                message = $"Property '{JsonNamingPolicy.CamelCase
                    .ConvertName(prop.Name)}' is not filterable";
                code = ValidationErrorCode.PropertyIsNotFilterable;
                return (null, null);
            }
            return (filterEx, attribute);
        }
        catch (ValidationException ex)
        {
            message = ex.Errors.FirstOrDefault().Value[0].message;
            code = Enum.Parse<ValidationErrorCode>(ex.Errors.FirstOrDefault().Value[0].code);
            return (null, null);
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return (null, null);
        }
    }

    public static string GetExpressionEndpoint<TSource, TDestintaion>(string sourceProperty, IConfigurationProvider provider)
    {
        string res = BaseDto.GetSource<TSource, TDestintaion>(sourceProperty, provider, throwException: false);
        return res;
    }

    public static FilterExpression GetFilterExpression<TSource, TDestintaion>(string filter, IConfigurationProvider provider)
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

    public static FilterableAttribute GetFilterAttribute<TSource, TDestintaion>
        (FilterExpression filterEx)
        where TSource : BaseEntity
        where TDestintaion : BaseDto
    {
        PropertyInfo prop = typeof(TDestintaion).GetProperties()
                .FirstOrDefault(p => p.Name == filterEx.Key)!;

        FilterableAttribute attribute = (FilterableAttribute)prop.GetCustomAttributes(true)
            .FirstOrDefault(a => a.GetType() == typeof(FilterableAttribute))!;
        if (attribute == null)
        {
            return null;
        }
        return attribute;
    }


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
        var filterEx = FilterExpression.Initialize<TSource, TDestintaion>(filter, provider);
        if (filterEx.ExpressionType == FilterExpressionType.Undefined)
            throw FiltersValidationException($"{filter} - expression is undefined",
                ValidationErrorCode.ExpressionIsUndefined);

        PropertyInfo prop = typeof(TDestintaion).GetProperties()
            .FirstOrDefault(p => p.Name == filterEx.Key)!;

        FilterableAttribute attribute = (FilterableAttribute)prop.GetCustomAttributes(true)
            .FirstOrDefault(a => a.GetType() == typeof(FilterableAttribute))!;
        if (attribute == null)
            throw FiltersValidationException($"Property " +
                $"'{JsonNamingPolicy.CamelCase.ConvertName(prop.Name)}' is not filterable",
                ValidationErrorCode.PropertyIsNotFilterable);

        return source.AppendToQuery<TSource, TDestintaion>(attribute.CompareMethod, filterEx);
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
        (this IQueryable<TSource> source, CompareMethod compareMethod, FilterExpression filterEx)
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
                expression = ByIdExpression(values, propExpression, filterEx.ExpressionType);
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
    private static Expression EqualExpression(object[] values, MemberExpression propExpression, FilterExpressionType expressionType)
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
        if (expressionType == FilterExpressionType.Include)
            return expression;
        else
            return Expression.Not(expression);
    }

    // Попытка убрать лишний join, нужно соблюдение указания foreign key
    /// <summary>
    /// Creates Equal() lambda expression by id from array of filter strings
    /// </summary>
    /// <param name="values">Filter strings</param>
    /// <param name="propExpression">A field of property</param>
    /// <param name="expressionType">Type of expression</param>
    /// <returns>Lambda expression with Equal() filter by id</returns>
    private static Expression ByIdExpression(object[] values, MemberExpression propExpression, FilterExpressionType expressionType)
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

    private static ValidationException FiltersValidationException(string message, string code)
    {
        return new ValidationException(
                    new Dictionary<string, ErrorItem[]> {
                        { "filters", [new ErrorItem(message, code)] }
                    }
                );
    }

    private static ValidationException FiltersValidationException(string message, ValidationErrorCode code)
    {
        return FiltersValidationException(message, code.ToString());
    }

    private static object ConvertFromString(this string value, Type type)
    {
        if (type == typeof(DateOnly) || type == typeof(DateOnly?))
            return DateOnly.Parse(value);
        return Convert.ChangeType(value, type);
    }

    private static object ConvertFromObject(object value, Type type)
        => value.ToString().ConvertFromString(type);
}