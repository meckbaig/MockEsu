using AutoMapper;
using FluentValidation;
using MockEsu.Application.Common.Dtos;
using MockEsu.Application.Common.Exceptions;
using MockEsu.Application.Common.Extensions.StringExtensions;
using MockEsu.Application.Extensions.ListFilters;
using MockEsu.Domain.Common;

namespace MockEsu.Application.Common.BaseRequests.ListQuery;

public abstract class BaseListQueryValidator<TQuery, TResponseList, TDestintaion, TSource> : AbstractValidator<TQuery>
    where TQuery : BaseListQuery<TResponseList>
    where TResponseList : BaseListQueryResponse<TDestintaion>
    where TDestintaion : class, IBaseDto
    where TSource : BaseEntity
{
    public BaseListQueryValidator(IMapper mapper)
    {
        RuleFor(x => x.skip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.take).GreaterThanOrEqualTo(0);
        RuleForEach(x => x.filters).MinimumLength(3)
            .ValidateFilterParsing<TQuery, TResponseList, TDestintaion, TSource>(mapper);
        RuleForEach(x => x.orderBy).MinimumLength(1)
            .ValidateSortParsing<TQuery, TResponseList, TDestintaion, TSource>(mapper);
    }
}

public static class BaseJournalQueryFilterValidatorExtension
{
    public static IRuleBuilderOptions<TQuery, string> ValidateFilterParsing
        <TQuery, TResponseList, TDestintaion, TSource>
        (this IRuleBuilderOptions<TQuery, string> ruleBuilder, IMapper mapper)
        where TQuery : BaseListQuery<TResponseList>
        where TResponseList : BaseListQueryResponse<TDestintaion>
        where TDestintaion : class, IBaseDto
        where TSource : BaseEntity
    {
        string key = string.Empty;
        ruleBuilder = ruleBuilder
            .Must((query, filter) => PropertyExists<TDestintaion>(filter, mapper.ConfigurationProvider, ref key))
            .WithMessage(x => $"Property '{key.ToCamelCase()}' does not exist")
            .WithErrorCode(ValidationErrorCode.PropertyDoesNotExistValidator.ToString());

        FilterExpression filterEx = null;
        ruleBuilder = ruleBuilder
            .Must((query, filter) => ExpressionIsValid<TDestintaion>(filter, mapper.ConfigurationProvider, ref filterEx))
            .WithMessage((query, filter) => $"{filter} - expression is undefined")
            .WithErrorCode(ValidationErrorCode.ExpressionIsUndefinedValidator.ToString());

        ruleBuilder = ruleBuilder
            .Must((query, filter) => PropertyIsFilterable<TDestintaion, TSource>(filterEx))
            .WithMessage((query, filter) => $"Property {filterEx.Key.ToCamelCase()}' is not filterable")
            .WithErrorCode(ValidationErrorCode.PropertyIsNotFilterableValidator.ToString());

        string expressionErrorMessage = string.Empty;
        ruleBuilder = ruleBuilder
            .Must((query, filter) => CanCreateExpression<TQuery, TResponseList, TDestintaion, TSource>
                                                        (query, filterEx, ref expressionErrorMessage))
            .WithMessage(x => expressionErrorMessage)
            .WithErrorCode(ValidationErrorCode.CanNotCreateExpressionValidator.ToString());

        return ruleBuilder;
    }

    private static bool PropertyExists<TDestintaion>(string filter, IConfigurationProvider provider, ref string key)
        where TDestintaion : class, IBaseDto
    {
        int expressionIndex;
        if (filter.Contains("!:"))
            expressionIndex = filter.IndexOf("!:");
        else if (filter.Contains(':'))
            expressionIndex = filter.IndexOf(":");
        else
            return true;
        string[] keySegments = filter[..expressionIndex].ToPropetyFormat().Split('.');
        Type type = typeof(TDestintaion);
        foreach (var segment in keySegments)
        {
            key = segment;
            string? endPoint = EntityFrameworkFiltersExtension
                .GetExpressionEndpoint(key, provider, type, out Type nextType);
            if (endPoint == null)
                return false;
            type = nextType;
        }
        return true;
    }

    private static bool ExpressionIsValid<TDestintaion>
        (string filter, IConfigurationProvider provider, ref FilterExpression filterEx)
        where TDestintaion : class, IBaseDto
    {
        filterEx = EntityFrameworkFiltersExtension.GetFilterExpression<TDestintaion>(filter, provider);
        if (filterEx?.ExpressionType == FilterExpressionType.Undefined)
            return false;
        return true;
    }

    private static bool PropertyIsFilterable<TDestintaion, TSource>
        (FilterExpression filterEx)
        where TDestintaion : IBaseDto
        where TSource : BaseEntity
    {
        if (filterEx == null || 
            filterEx.EndPoint == null || 
            filterEx.ExpressionType == FilterExpressionType.Undefined)
            return true;
        return EntityFrameworkFiltersExtension
            .TryGetFilterAttributes<TDestintaion>(filterEx);
    }

    private static bool CanCreateExpression<TQuery, TResponseList, TDestintaion, TSource>
        (TQuery query, FilterExpression? filterEx, ref string errorMessage)
        where TQuery : BaseListQuery<TResponseList>
        where TResponseList : BaseListQueryResponse<TDestintaion>
        where TDestintaion : IBaseDto
        where TSource : BaseEntity
    {

        if (filterEx == null ||
            filterEx.ExpressionType == FilterExpressionType.Undefined ||
            filterEx?.CompareMethod == null)
        {
            return true;
        }
        try
        {
            if (!EntityFrameworkFiltersExtension.TryGetLinqExpression(filterEx, out var expression))
                return false;
            query.AddFilterExpression(expression);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }
}

public static class BaseJournalQuerySortValidatorExtension
{
    public static IRuleBuilderOptions<TQuery, string> ValidateSortParsing
        <TQuery, TResponseList, TDestintaion, TSource>
        (this IRuleBuilderOptions<TQuery, string> ruleBuilder, IMapper mapper)
        where TQuery : BaseListQuery<TResponseList>
        where TResponseList : BaseListQueryResponse<TDestintaion>
        where TDestintaion : IBaseDto
        where TSource : BaseEntity
    {
        string key = string.Empty;
        string? endPoint = null;
        ruleBuilder = ruleBuilder
            .Must((query, filter) => PropertyExists<TSource, TDestintaion>(filter, mapper.ConfigurationProvider, ref key, out endPoint))
            .WithMessage(x => $"Property '{key.ToCamelCase()}' does not exist")
            .WithErrorCode(ValidationErrorCode.PropertyDoesNotExistValidator.ToString());

        OrderByExpression orderByEx = null;
        ruleBuilder = ruleBuilder
            .Must((query, filter) => 
            {
                if (endPoint == null)
                    return false;
                return ExpressionIsValid<TQuery, TResponseList, TDestintaion, TSource>
                    (filter, mapper.ConfigurationProvider, out orderByEx);
            })
            .WithMessage((query, filter) => $"{filter} - expression is undefined")
            .WithErrorCode(ValidationErrorCode.ExpressionIsUndefinedValidator.ToString());

        string expressionErrorMessage = string.Empty;
        ruleBuilder = ruleBuilder
            .Must((query, filter) => CanCreateExpression<TQuery, TResponseList, TDestintaion, TSource>
                    (query, orderByEx, ref expressionErrorMessage))
            .WithMessage(x => expressionErrorMessage)
            .WithErrorCode(ValidationErrorCode.CanNotCreateExpressionValidator.ToString());

        return ruleBuilder;
    }

    private static bool PropertyExists<TSource, TDestintaion>(
        string filter,
        IConfigurationProvider provider,
        ref string key,
        out string? endPoint)
    {
        if (filter.Contains(' '))
            key = filter[..filter.IndexOf(' ')].ToPascalCase();
        else
            key = filter.ToPascalCase();
        endPoint = EntityFrameworkOrderByExtension
            .GetExpressionEndpoint<TSource, TDestintaion>(key, provider);
        if (endPoint == null)
            return false;
        return true;
    }

    private static bool ExpressionIsValid
        <TQuery, TResponseList, TDestintaion, TSource>
        (string filter, IConfigurationProvider provider, out OrderByExpression orderByEx)
        where TQuery : BaseListQuery<TResponseList>
        where TResponseList : BaseListQueryResponse<TDestintaion>
        where TDestintaion : IBaseDto
        where TSource : BaseEntity
    {
        orderByEx = EntityFrameworkOrderByExtension.GetOrderByExpression<TSource, TDestintaion>(filter, provider);
        if (orderByEx?.ExpressionType == OrderByExpressionType.Undefined)
            return false;
        return true;
    }

    private static bool CanCreateExpression<TQuery, TResponseList, TDestintaion, TSource>
        (TQuery query, OrderByExpression? orderByEx, ref string errorMessage)
        where TQuery : BaseListQuery<TResponseList>
        where TResponseList : BaseListQueryResponse<TDestintaion>
        where TDestintaion : IBaseDto
        where TSource : BaseEntity
    {

        if (orderByEx == null ||
            orderByEx.ExpressionType == OrderByExpressionType.Undefined)
        {
            return true;
        }
        try
        {
            if (!EntityFrameworkOrderByExtension.TryGetLinqExpression<TSource>(orderByEx, out Expression expression))
            return false;
            query.AddOrderExpression(expression);
        return true;
    }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }
}
