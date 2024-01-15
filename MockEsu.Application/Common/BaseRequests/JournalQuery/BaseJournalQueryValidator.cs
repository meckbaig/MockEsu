using AutoMapper;
using FluentValidation;
using MockEsu.Application.Common.Attributes;
using MockEsu.Application.Common.Exceptions;
using MockEsu.Application.Extensions.JournalFilters;
using MockEsu.Domain.Common;
using System.Text.Json;

namespace MockEsu.Application.Common.BaseRequests.JournalQuery
{
    public class BaseJournalQueryValidator<TQuery, TResponseList, TResponse, TSource> : AbstractValidator<TQuery>
        where TQuery : BaseListQuery<TResponseList>
        where TResponseList : BaseListQueryResponse<TResponse>
        where TResponse : BaseDto
        where TSource : BaseEntity
    {
        public BaseJournalQueryValidator(IMapper mapper)
        {
            RuleFor(x => x.skip).GreaterThanOrEqualTo(0);
            RuleFor(x => x.take).GreaterThanOrEqualTo(0);
            RuleForEach(x => x.filters).MinimumLength(3)
                .ValidateFilterParsing<TQuery, TResponseList, TResponse, TSource>(mapper);
        }
    }

    public static class BaseJournalQueryFilterValidatorExtension
    {
        public static IRuleBuilderOptions<TQuery, string> ValidateFilterParsing
            <TQuery, TResponseList, TResponse, TSource>
            (this IRuleBuilderOptions<TQuery, string> ruleBuilder, IMapper mapper)
            where TQuery : BaseListQuery<TResponseList>
            where TResponseList : BaseListQueryResponse<TResponse>
            where TResponse : BaseDto
            where TSource : BaseEntity
        {
            string key = string.Empty;
            ruleBuilder = ruleBuilder
                .Must((query, filter) => PropertyExists<TSource, TResponse>(filter, mapper.ConfigurationProvider, ref key))
                .WithMessage(x => $"Property '{JsonNamingPolicy.CamelCase.ConvertName(key)}' does not exist")
                .WithErrorCode(ValidationErrorCode.PropertyDoesNotExist.ToString());

            FilterExpression filterEx = null;
            ruleBuilder = ruleBuilder
                .Must((query, filter) => ExpressionIsValid<TSource, TResponse>(filter, mapper.ConfigurationProvider, ref filterEx))
                .WithMessage((query, filter) => $"{filter} - expression is undefined")
                .WithErrorCode(ValidationErrorCode.ExpressionIsUndefined.ToString());

            ruleBuilder = ruleBuilder
                .Must((query, filter) => PropertyIsFilterable<TQuery, TResponseList, TResponse, TSource>(filterEx, query))
                .WithMessage((query, filter) => $"Property " +
                    $"'{JsonNamingPolicy.CamelCase.ConvertName(filterEx.Key)}' is not filterable")
                .WithErrorCode(ValidationErrorCode.PropertyIsNotFilterable.ToString());

            return ruleBuilder;
        }

        private static bool PropertyExists<TSource, TDestintaion>(string filter, IConfigurationProvider provider, ref string key)
            where TSource : BaseEntity
            where TDestintaion : BaseDto
        {
            int expressionIndex;
            if (filter.Contains("!:"))
                expressionIndex = filter.IndexOf("!:");
            else if (filter.Contains(':'))
                expressionIndex = filter.IndexOf(":");
            else
                return true;
            key = FilterExpression.ToPascalCase(filter[..expressionIndex]);

            string endPoint = EntityFrameworkFiltersExtension
                .GetExpressionEndpoint<TSource, TDestintaion>(key, provider);
            if (endPoint == null)
                return false;
            return true;
        }

        private static bool ExpressionIsValid<TSource, TDestintaion>(string filter, IConfigurationProvider provider, ref FilterExpression filterEx)
            where TSource : BaseEntity
            where TDestintaion : BaseDto
        {
            filterEx = EntityFrameworkFiltersExtension.GetFilterExpression<TSource, TDestintaion>(filter, provider);
            if (filterEx?.ExpressionType == FilterExpressionType.Undefined)
                return false;
            return true;
        }

        private static bool PropertyIsFilterable<TQuery, TResponseList, TDestintaion, TSource>(FilterExpression filterEx, TQuery query)
            where TQuery : BaseListQuery<TResponseList>
            where TResponseList : BaseListQueryResponse<TDestintaion>
            where TDestintaion : BaseDto
            where TSource : BaseEntity
        {
            if (filterEx == null || filterEx.ExpressionType == FilterExpressionType.Undefined)
                return true;
            FilterableAttribute attribute = EntityFrameworkFiltersExtension
                .GetFilterAttribute<TSource, TDestintaion>(filterEx);
            if (attribute == null)
                return false;
            query.AddFilterExpression(filterEx, attribute);
            return true;
        }
    }

    public static class BaseJournalQuerySortValidatorExtension
    {

    }
}
