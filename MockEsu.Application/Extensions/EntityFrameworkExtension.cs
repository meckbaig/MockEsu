using AutoMapper;
using MockEsu.Application.Common;
using MockEsu.Application.Common.Attributes;
using System.Globalization;
using System.Reflection;

namespace MockEsu.Infrastructure.Extensions;

public static class EntityFrameworkExtension
{
    public static IQueryable<TSource> AddFilters<TSource, TDestintaion>
        (this IQueryable<TSource> source, IMapper mapper, string[] filters) where TDestintaion : BaseDto
    {
        var properties = new Dictionary<string, string>();
        IConfigurationProvider provider = mapper.ConfigurationProvider;

        foreach (var filter in filters)
        {
            source.AddFilter<TSource, TDestintaion>(provider, filter);
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
        (this IQueryable<TSource> source, IConfigurationProvider provider, string filter) where TDestintaion : BaseDto
    {
        var filterEx = new FilterExpression(filter);
        if (filterEx.Expression == Expression.Undefined)
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

    private static IQueryable<TSource> AppendToQuery<TSource>(IQueryable<TSource> source, CompareMethod compareMethod, FilterExpression filterEx)
    {
        switch (compareMethod)
        {
            case CompareMethod.Equals:
                break;
            case CompareMethod.Between:
                break;
            case CompareMethod.CellContainsValue:
                break;
            case CompareMethod.ValueContainsCell:
                break;
            case CompareMethod.DateTime:
                break;
            case CompareMethod.Date:
                break;
            case CompareMethod.ById:
                break;
            default:
                break;
        }
        return source;
    }

    private record FilterExpression
    {
        public string Key { get; set; }
        public Expression Expression { get; set; }
        public string Value { get; set; }

        public FilterExpression(string filter)
        {
            if (filter.Contains("!:"))
            {
                Key = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(filter.Substring(0, filter.IndexOf("!:")));
                Value = filter.Substring(filter.IndexOf("!:"));
                Expression = Expression.Exclude;
            }
            else if (filter.Contains(":"))
            {
                Key = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(filter.Substring(0, filter.IndexOf(":")));
                Value = filter.Substring(filter.IndexOf(":"));
                Expression = Expression.Include;
            }
            else
                Expression = Expression.Undefined;
        }
    }

    private enum Expression
    {
        Include, Exclude, Undefined
    }
}