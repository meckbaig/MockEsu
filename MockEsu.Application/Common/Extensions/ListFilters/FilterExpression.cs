﻿using AutoMapper;
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

    /// <summary>
    /// Type of expression
    /// </summary>
    public FilterExpressionType ExpressionType { get; set; }

    /// <summary>
    /// Filter value
    /// </summary>
    public string? Value { get; set; }

    public FilterExpression? Nested { get; set; }

    /// <summary>
    /// Factory constructor
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TDestintaion"></typeparam>
    /// <param name="filter"></param>
    /// <param name="provider"></param>
    /// <returns></returns>
    public static FilterExpression Initialize<TSource, TDestintaion>(string filter, IConfigurationProvider provider)
        where TSource : BaseEntity
        where TDestintaion : class, IBaseDto
    {
        var f = new FilterExpression();
        if (filter.Contains("!:"))
        {
            string filterPath = filter[..filter.IndexOf("!:")];
            f.Key = filterPath.Split('.')[0].ToPascalCase();
            f.EndPoint = EntityFrameworkFiltersExtension.GetExpressionEndpoint<TDestintaion>(f.Key, provider, out Type propertyType);
            f.Value = filter[(filter.IndexOf("!:") + 2)..];
            f.ExpressionType = FilterExpressionType.Exclude;
        }
        else if (filter.Contains(':'))
        {
            f.Key = filter[..filter.IndexOf(':')].ToPropetyFormat();
            f.EndPoint = EntityFrameworkFiltersExtension.GetExpressionEndpoint<TDestintaion>(f.Key, provider, out Type propertyType);
            f.Value = filter[(filter.IndexOf(':') + 1)..];
            f.ExpressionType = FilterExpressionType.Include;
        }
        else
            f.ExpressionType = FilterExpressionType.Undefined;

        return f;
    }
}

public enum FilterExpressionType
{
    Include, Exclude, Undefined
}