using AutoMapper;
using MockEsu.Application.Common.Dtos;
using MockEsu.Application.Extensions.JsonPatch;
using MockEsu.Application.Extensions.ListFilters;
using MockEsu.Application.Common.Extensions.StringExtensions;
using MockEsu.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MockEsu.Application.Common.Attributes;

namespace MockEsu.Application.Extensions.ListFilters;

/// <summary>
/// Class representing a filtering expression
/// </summary>
/// <typeparam name="TSource">Source of DTO type</typeparam>
/// <typeparam name="TDestintaion">DTO type</typeparam>
public record FilterExpression : IEntityFrameworkExpression<FilterExpressionType>
{
    /// <summary>
    /// DTO key
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Source endpoint key
    /// </summary>
    public string? EndPoint { get; set; }

    public Type EntityType { get; set; }

    /// <summary>
    /// Type of expression
    /// </summary>
    public FilterExpressionType ExpressionType { get; set; }

    /// <summary>
    /// Filter value
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Nested filter expression (if property is a collection)
    /// </summary>
    public FilterExpression? InnerFilterExpression { get; set; }

    public CompareMethod CompareMethod { get; set; }

    /// <summary>
    /// Factory constructor
    /// </summary>
    /// <typeparam name="TDestintaion"></typeparam>
    /// <param name="filter"></param>
    /// <param name="provider"></param>
    /// <returns></returns>
    public static FilterExpression Initialize<TDestintaion>(string filter, IConfigurationProvider provider)
        where TDestintaion : class, IBaseDto
    {
        var f = new FilterExpression { EntityType = TDestintaion.GetOriginType() };
        if (filter.Contains("!:"))
        {
            string filterPath = filter[..filter.IndexOf("!:")];
            string[] segments = filterPath.Split('.');
            f.Key = segments[0].ToPascalCase();
            f.EndPoint = EntityFrameworkFiltersExtension.GetExpressionEndpoint(f.Key, provider, typeof(TDestintaion), out Type propertyType);
            f.Value = filter[(filter.IndexOf("!:") + 2)..];
            f.ExpressionType = FilterExpressionType.Exclude;
            if (segments.Length > 1)
                f.InnerFilterExpression = InvokeInitialize($"{string.Join('.', segments[1..])}!:{f.Value}", provider, propertyType);
        }
        else if (filter.Contains(':'))
        {
            string filterPath = filter[..filter.IndexOf(":")];
            string[] segments = filterPath.Split('.');
            f.Key = segments[0].ToPascalCase();
            f.EndPoint = EntityFrameworkFiltersExtension.GetExpressionEndpoint(f.Key, provider, typeof(TDestintaion), out Type propertyType);
            f.Value = filter[(filter.IndexOf(':') + 1)..];
            f.ExpressionType = FilterExpressionType.Include;
            if (segments.Length > 1)
                f.InnerFilterExpression = InvokeInitialize($"{string.Join('.', segments[1..])}:{f.Value}", provider, propertyType);
        }
        else
            f.ExpressionType = FilterExpressionType.Undefined;
        return f;
    }

    private static FilterExpression InvokeInitialize(string filter, IConfigurationProvider provider, Type propertyType)
    {
        var methodInfo = typeof(FilterExpression).GetMethod(nameof(Initialize));
        var genericMethod = methodInfo.MakeGenericMethod(propertyType);
        object[] parameters = [filter, provider];
        return (FilterExpression)genericMethod.Invoke(null, parameters);
    }
}

public enum FilterExpressionType
{
    Include, Exclude, Undefined
}
