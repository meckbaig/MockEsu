using AutoMapper;
using MockEsu.Application.Common;
using MockEsu.Application.Common.Attributes;
using MockEsu.Application.DTOs.Kontragents;
using MockEsu.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Infrastructure.Extensions;

public static class EntityFrameworkExtension
{
    public static IQueryable<TSource> AddFilters<TSource, TDestintaion>(this IQueryable<TSource> source, IMapper mapper, Span<string> filters) where TDestintaion : BaseDto
    {
        var properties = new Dictionary<string, string>();
        var provider = mapper.ConfigurationProvider;
        foreach (var prop in typeof(TDestintaion).GetProperties())
        {
            ///TODO: здесь проверять на вхождение в фильтры, на способ поиска и добавлять в query требуемые фильтры
            FilterableAttribute attribute = (FilterableAttribute)prop.GetCustomAttributes(true).FirstOrDefault(a => a.GetType() == typeof(FilterableAttribute));
            if (attribute != null)
                properties.Add(prop.Name, BaseDto.GetSource<Kontragent, KonragentPreviewDto>(provider, prop.Name));
        }
        return source;
    }
}
