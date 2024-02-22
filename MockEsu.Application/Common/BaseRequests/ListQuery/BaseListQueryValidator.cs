using AutoMapper;
using FluentValidation;
using MockEsu.Application.Common.Attributes;
using MockEsu.Application.Common.Dtos;
using MockEsu.Application.Common.Exceptions;
using MockEsu.Application.Extensions.ListFilters;
using MockEsu.Application.Common.Extensions.StringExtensions;
using MockEsu.Domain.Common;
using System.Text.Json;

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
            .Must((query, filter) => PropertyExists<TSource, TDestintaion>(filter, mapper.ConfigurationProvider, ref key))
            .WithMessage(x => $"Property '{key.ToCamelCase()}' does not exist")
            .WithErrorCode(ValidationErrorCode.PropertyDoesNotExistValidator.ToString());

        FilterExpression filterEx = null;
        ruleBuilder = ruleBuilder
            .Must((query, filter) => ExpressionIsValid<TSource, TDestintaion>(filter, mapper.ConfigurationProvider, ref filterEx))
            .WithMessage((query, filter) => $"{filter} - expression is undefined")
            .WithErrorCode(ValidationErrorCode.ExpressionIsUndefinedValidator.ToString());

        FilterableAttribute attribute = null;
        ruleBuilder = ruleBuilder
            .Must((query, filter) => PropertyIsFilterable<TDestintaion, TSource>(filterEx, ref attribute))
            .WithMessage((query, filter) => $"Property {filterEx.Key.ToCamelCase()}' is not filterable")
            .WithErrorCode(ValidationErrorCode.PropertyIsNotFilterableValidator.ToString());

        string expressionErrorMessage = string.Empty;
        ruleBuilder = ruleBuilder
            .Must((query, filter) => CanCreateExpression<TQuery, TResponseList, TDestintaion, TSource>(query, filterEx, attribute, ref expressionErrorMessage))
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
        key = filter[..expressionIndex].ToPascalCase();

        string? endPoint = EntityFrameworkFiltersExtension
            .GetExpressionEndpoint<TSource, TDestintaion>(key, provider);
        if (endPoint == null)
            return false;
        return true;
    }

    private static bool ExpressionIsValid<TSource, TDestintaion>
        (string filter, IConfigurationProvider provider, ref FilterExpression filterEx)
        where TSource : BaseEntity
        where TDestintaion : class, IBaseDto
    {
        filterEx = EntityFrameworkFiltersExtension.GetFilterExpression<TSource, TDestintaion>(filter, provider);
        if (filterEx?.ExpressionType == FilterExpressionType.Undefined)
            return false;
        return true;
    }

    private static bool PropertyIsFilterable<TDestintaion, TSource>
        (FilterExpression filterEx, ref List<FilterableAttribute> attributes)
        where TDestintaion : IBaseDto
        where TSource : BaseEntity
    {
        if (filterEx == null || filterEx.ExpressionType == FilterExpressionType.Undefined)
            return true;
        attribute = EntityFrameworkFiltersExtension
            .GetFilterAttribute<TDestintaion>(filterEx.Key);
        if (attribute == null)
            return false;
        return true;
    }

    private static bool CanCreateExpression<TQuery, TResponseList, TDestintaion, TSource>
        (TQuery query, FilterExpression? filterEx, FilterableAttribute? attribute, ref string errorMessage)
        where TQuery : BaseListQuery<TResponseList>
        where TResponseList : BaseListQueryResponse<TDestintaion>
        where TDestintaion : IBaseDto
        where TSource : BaseEntity
    {

        if (filterEx == null ||
            filterEx.ExpressionType == FilterExpressionType.Undefined ||
            attribute == null)
        {
            return true;
        }
        try
        {
            var expression = EntityFrameworkFiltersExtension
                .GetLinqExpression<TSource>(attribute.CompareMethod, filterEx);
            if (expression == null)
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
        ruleBuilder = ruleBuilder
            .Must((query, filter) => PropertyExists<TSource, TDestintaion>(filter, mapper.ConfigurationProvider, ref key))
            .WithMessage(x => $"Property '{key.ToCamelCase()}' does not exist")
            .WithErrorCode(ValidationErrorCode.PropertyDoesNotExistValidator.ToString());

        ruleBuilder = ruleBuilder
            .Must((query, filter) => ExpressionIsValid<TQuery, TResponseList, TDestintaion, TSource>
            (query, filter, mapper.ConfigurationProvider))
            .WithMessage((query, filter) => $"{filter} - expression is undefined")
            .WithErrorCode(ValidationErrorCode.ExpressionIsUndefinedValidator.ToString());

        return ruleBuilder;
    }

    private static bool PropertyExists<TSource, TDestintaion>(string filter, IConfigurationProvider provider, ref string key)
    {
        if (filter.Contains(' '))
            key = filter[..filter.IndexOf(' ')].ToPascalCase();
        else
            key = filter.ToPascalCase();
        string endPoint = EntityFrameworkOrderByExtension
            .GetExpressionEndpoint<TSource, TDestintaion>(key, provider);
        if (endPoint == null)
            return false;
        return true;
    }

    private static bool ExpressionIsValid
        <TQuery, TResponseList, TDestintaion, TSource>
        (TQuery query, string filter, IConfigurationProvider provider)
        where TQuery : BaseListQuery<TResponseList>
        where TResponseList : BaseListQueryResponse<TDestintaion>
        where TDestintaion : IBaseDto
        where TSource : BaseEntity
    {
        OrderByExpression ex = EntityFrameworkOrderByExtension.GetOrderByExpression<TSource, TDestintaion>(filter, provider);
        if (ex?.ExpressionType == OrderByExpressionType.Undefined)
            return false;
        if (ex != null) 
            query.AddOrderExpression(ex);
        return true;
    }
}
