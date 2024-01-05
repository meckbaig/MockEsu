using AutoMapper;
using MockEsu.Application.Common;
using MockEsu.Application.Common.Attributes;
using MockEsu.Application.DTOs.Kontragents;
using MockEsu.Domain.Common;
using MockEsu.Domain.Entities;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace MockEsu.Infrastructure.Extensions;

public static class EntityFrameworkExtension
{
    public static IQueryable<TSource> AddFilters<TSource, TDestintaion>
        (this IQueryable<TSource> source, IMapper mapper, string[]? filters)
        where TDestintaion : BaseDto
        where TSource : BaseEntity
    {
        if (filters == null)
            return source;
        var properties = new Dictionary<string, string>();
        IConfigurationProvider provider = mapper.ConfigurationProvider;

        foreach (var filter in filters)
        {
            source = source.AddFilter<TSource, TDestintaion>(provider, filter);
        }


        //foreach (var prop in typeof(TDestintaion).GetProperties())
        //{
        //    ///TODO: здесь проверять на вхождение в фильтры, на способ поиска и добавлять в query требуемые фильтры
        //    FilterableAttribute attribute = (FilterableAttribute)prop.GetCustomAttributes(true)
        //        .FirstOrDefault(a => a.GetType() == typeof(FilterableAttribute));
        //    if (attribute != null)
        //        properties.Add(prop.Name, BaseDto.GetSource<Kontragent, KonragentPreviewDto>(provider, prop.Name));
        //}
        return source;
    }

    private static IQueryable<TSource> AddFilter<TSource, TDestintaion>
        (this IQueryable<TSource> source, IConfigurationProvider provider, string filter)
        where TDestintaion : BaseDto
        where TSource : BaseEntity
    {
        var filterEx = new FilterExpression(filter, provider);
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


        //properties.Add(prop.Name, BaseDto.GetSource<TSource, TDestintaion>(provider, prop.Name));


        return AppendToQuery(source, attribute.CompareMethod, filterEx);
    }

    /// <summary>
    /// Compare method selector
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="source"></param>
    /// <param name="compareMethod"></param>
    /// <param name="filterEx"></param>
    /// <returns></returns>
    private static IQueryable<TSource> AppendToQuery<TSource>
        (IQueryable<TSource> source, CompareMethod compareMethod, FilterExpression filterEx) where TSource : BaseEntity
    {
        switch (compareMethod)
        {
            case CompareMethod.Equals:
                //string[] filters = filterEx.Value.Split(',');
                //source = source.Where(s => s.Id.Equals(filters[0]));
                source = AppendEqual(source, filterEx);
                break;
            default:
                break;
        }
        return source;
    }

    private static IQueryable<TSource> AppendEqual<TSource>
        (IQueryable<TSource> source, FilterExpression filterEx) where TSource : BaseEntity
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

        object value = ConvertFromObject(values[0], propExpression.Type);
        Expression expression = Expression.Equal(propExpression, Expression.Constant(value, propExpression.Type));
        if (values.Length != 1)
        {
            for (int i = 1; i < values.Length; i++)
            {
                value = ConvertFromObject(values[i], propExpression.Type);
                expression = Expression.OrElse(
                        expression, 
                        Expression.Equal(propExpression, Expression.Constant(value, propExpression.Type))
                    );
            }
        }

        Expression<Func<TSource, bool>> filterLambda = Expression.Lambda<Func<TSource, bool>>(
                expression,
                param
            );

        return source.Where(filterLambda);
    }

    private record FilterExpression
    {
        public string Key { get; set; }
        public string EndPoint { get; set; }
        public ExpressionType ExpressionType { get; set; }
        public string Value { get; set; }

        public FilterExpression(string filter, IConfigurationProvider provider)
        {
            if (filter.Contains("!:"))
            {
                Key = ToPascalCase(filter.Substring(0, filter.IndexOf(":")));
                EndPoint = BaseDto.GetSource<Kontragent, KonragentPreviewDto>(provider, Key);
                Value = filter.Substring(filter.IndexOf("!:") + 2);
                ExpressionType = ExpressionType.Exclude;
            }
            else if (filter.Contains(":"))
            {
                Key = ToPascalCase(filter.Substring(0, filter.IndexOf(":")));
                EndPoint = BaseDto.GetSource<Kontragent, KonragentPreviewDto>(provider, Key);
                Value = filter.Substring(filter.IndexOf(":") + 1);
                ExpressionType = ExpressionType.Include;
            }
            else
                ExpressionType = ExpressionType.Undefined;
        }

        string ToPascalCase(string value)
        {
            if (value.Length <= 1)
                return value.ToUpper();
            return $"{value[0].ToString().ToUpper()}{value.Substring(1)}";
        }
    }

    public static object ConvertFromString(this string value, Type type)
    {
        if (type == typeof(DateOnly))
            return DateOnly.Parse(value);
        if (type == typeof(DateOnly?))
            return (DateOnly?)DateOnly.Parse(value);
        return Convert.ChangeType(value, type);
    }

    public static object ConvertFromObject(object value, Type type)
        => value.ToString().ConvertFromString(type);
    

    private enum ExpressionType
    {
        Include, Exclude, Undefined
    }
}