﻿using AutoMapper;
using MockEsu.Application.Common.Dtos;
using MockEsu.Application.Common.Extensions.StringExtensions;
using MockEsu.Application.Extensions.JsonPatch;
using MockEsu.Domain.Common;

namespace MockEsu.Application.Extensions.ListFilters;

public record OrderByExpression : IEntityFrameworkExpression<OrderByExpressionType>
{
    public string? Key { get; set; }
    public string? EndPoint { get; set; }
    public OrderByExpressionType ExpressionType { get; set; }

    public static OrderByExpression Initialize<TSource, TDestintaion>(string filter, IConfigurationProvider provider)
        where TSource : BaseEntity
        where TDestintaion : IBaseDto
    {
        var f = new OrderByExpression();

        if (!filter.Contains(' '))
        {
            f.Key = filter.ToPascalCase();
            f.EndPoint = DtoExtension.GetSource<TSource, TDestintaion>(f.Key, provider);
            f.ExpressionType = OrderByExpressionType.Ascending;
        }
        else if (filter[(filter.IndexOf(' ') + 1)..] == "desc")
        {
            f.Key = filter[..filter.IndexOf(' ')].ToPascalCase();
            f.EndPoint = DtoExtension.GetSource<TSource, TDestintaion>(f.Key, provider);
            f.ExpressionType = OrderByExpressionType.Descending;
        }
        else
            f.ExpressionType = OrderByExpressionType.Undefined;
        return f;
    }
}

public enum OrderByExpressionType
{
    Ascending, Descending, Undefined
}
