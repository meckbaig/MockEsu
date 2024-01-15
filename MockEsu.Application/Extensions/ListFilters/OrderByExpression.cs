using AutoMapper;
using MockEsu.Application.Common;
using MockEsu.Application.Extensions.JournalFilters;
using MockEsu.Domain.Common;

namespace MockEsu.Application.Extensions.ListFilters;

public record OrderByExpression : EntityFrameworkExpression<OrderByExpressionType>
{
    public string? Key { get; set; }
    public string? EndPoint { get; set; }
    public OrderByExpressionType ExpressionType { get; set; }

    public static OrderByExpression Initialize<TSource, TDestintaion>(string filter, IConfigurationProvider provider)
        where TSource : BaseEntity
        where TDestintaion : BaseDto
    {
        var f = new OrderByExpression();

        if (!filter.Contains(' '))
        {
            f.Key = ToPascalCase(filter);
            f.EndPoint = BaseDto.GetSource<TSource, TDestintaion>(f.Key, provider);
            f.ExpressionType = OrderByExpressionType.Ascending;
        }
        else if (filter[(filter.IndexOf(' ') + 1)..] == "desc")
        {
            f.Key = ToPascalCase(filter[..filter.IndexOf(' ')]);
            f.EndPoint = BaseDto.GetSource<TSource, TDestintaion>(f.Key, provider);
            f.ExpressionType = OrderByExpressionType.Descending;
        }
        else
            f.ExpressionType = OrderByExpressionType.Undefined;
        return f;
    }

    /// <summary>
    /// Converts a string to pascal case
    /// </summary>
    /// <param name="value">input string</param>
    /// <returns>String in pascal case</returns>
    public static string ToPascalCase(string value)
    {
        if (value.Length <= 1)
            return value.ToUpper();
        return $"{value[0].ToString().ToUpper()}{value.Substring(1)}";
    }
}

public enum OrderByExpressionType
{
    Ascending, Descending, Undefined
}
