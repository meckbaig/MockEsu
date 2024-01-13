using AutoMapper;
using FluentValidation;
using MockEsu.Application.Common.Attributes;
using MockEsu.Application.Common.Exceptions;
using MockEsu.Application.Extensions.JournalFilters;
using MockEsu.Domain.Common;

namespace MockEsu.Application.Common.BaseRequests.JournalQuery
{
    public class BaseJournalQueryValidator<TQuery, TResponseList, TResponse, TSourse> : AbstractValidator<TQuery>
        where TQuery : BaseListQuery<TResponseList>
        where TResponseList : BaseListQueryResponse<TResponse>
        where TResponse : BaseDto
        where TSourse : BaseEntity
    {
        public BaseJournalQueryValidator(IMapper mapper)
        {
            RuleFor(x => x.skip).GreaterThanOrEqualTo(0);
            RuleFor(x => x.take).GreaterThanOrEqualTo(0);
            RuleForEach(x => x.filters).MinimumLength(3)
                .CanParseFilters<TQuery, TResponseList, TResponse, TSourse>(mapper);
        }
    }

    public static class BaseJournalQueryValidatorExtensions
    {
        private static bool ValidateFilter<TQuery, TResponseList, TResponse, TSourse>
            (string filter, TQuery query, IMapper mapper, ref string message, ref ValidationErrorCode code)
            where TQuery : BaseListQuery<TResponseList>
            where TResponseList : BaseListQueryResponse<TResponse>
            where TResponse : BaseDto
            where TSourse : BaseEntity
        {
            (FilterExpression filterEx, FilterableAttribute attribute) = EntityFrameworkFiltersExtension
                .ParseFilterToExpression<TSourse, TResponse>(mapper.ConfigurationProvider, filter, ref message, ref code);
            if (filterEx == null)
                return false;
            query.AddFilterExpression(filterEx, attribute);
            return true;
        }

        public static IRuleBuilderOptions<TQuery, string> CanParseFilters
            <TQuery, TResponseList, TResponse, TSourse>
            (this IRuleBuilderOptions<TQuery, string> ruleBuilder, IMapper mapper)
            where TQuery : BaseListQuery<TResponseList>
            where TResponseList : BaseListQueryResponse<TResponse>
            where TResponse : BaseDto
            where TSourse : BaseEntity
        {
            string message = "Can not find validation error message";
            ValidationErrorCode code = ValidationErrorCode.NotSpecifiedValidationError;
            return ruleBuilder.Must((query, filter)
                => ValidateFilter<TQuery, TResponseList, TResponse, TSourse>
                    (filter, query, mapper, ref message, ref code))
                    .WithMessage(x => message)
                    .WithErrorCode(x => code.ToString());
        }

        public static IRuleBuilderOptions<T, TProperty> WithErrorCode<T, TProperty>
            (this IRuleBuilderOptions<T, TProperty> rule, Func<T, string> errorProvider)
        {
            DefaultValidatorOptions.Configurable(rule).Current.ErrorCode = errorProvider.ToString();
            return rule;
        }
    }
}
