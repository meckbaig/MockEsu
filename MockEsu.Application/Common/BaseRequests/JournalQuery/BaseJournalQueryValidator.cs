using AutoMapper;
using FluentValidation;
using MockEsu.Application.Common.Attributes;
using MockEsu.Application.Common.Exceptions;
using MockEsu.Application.Extensions.JournalFilters;
using MockEsu.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace MockEsu.Application.Common.BaseRequests.JournalQuery
{
    public class BaseJournalQueryValidator<TQuery, TResponseJournal, TResponse, TSourse> : AbstractValidator<TQuery> 
        where TQuery : BaseJournalQuery<TResponseJournal>
        where TResponseJournal : BaseJournalQueryResponse<TResponse>
        where TResponse : BaseDto
        where TSourse : BaseEntity
    {
        public BaseJournalQueryValidator(IMapper mapper)
        {
            RuleFor(x => x.skip).GreaterThanOrEqualTo(0);
            RuleFor(x => x.take).GreaterThanOrEqualTo(0);
            RuleForEach(x => x.filters).MinimumLength(3)
                .CanParseFilters<TQuery, TResponseJournal, TResponse, TSourse>(mapper);
        }
    }

    public static class BaseJournalQueryValidatorExtensions
    {
        private static bool ValidateFilter<TQuery, TResponseJournal, TResponse, TSourse>
            (string filter, TQuery query, IMapper mapper, ref string message, ref ValidationErrorCode code)
            where TQuery : BaseJournalQuery<TResponseJournal>
            where TResponseJournal : BaseJournalQueryResponse<TResponse>
            where TResponse : BaseDto
            where TSourse : BaseEntity
        {
            (FilterExpression filterEx, FilterableAttribute attribute) = EntityFrameworkFiltersExtension
                .ParseFilterToExpression<TSourse,TResponse>(mapper.ConfigurationProvider, filter, ref message, ref code);
            if (filterEx == null)
                return false;
            query.AddFilterExpression(filterEx, attribute);
            return true;
        }

        public static IRuleBuilderOptions<TQuery, string> CanParseFilters
            <TQuery, TResponseJournal, TResponse, TSourse>
            (this IRuleBuilderOptions<TQuery, string> ruleBuilder, IMapper mapper)
            where TQuery : BaseJournalQuery<TResponseJournal>
            where TResponseJournal : BaseJournalQueryResponse<TResponse>
            where TResponse : BaseDto
            where TSourse : BaseEntity
        {
            string message = "Can not find validation error message";
            ValidationErrorCode code = ValidationErrorCode.NotSpecifiedValidationError;
            return ruleBuilder.Must((query, filter)
                => ValidateFilter<TQuery, TResponseJournal, TResponse, TSourse>
                    (filter, query, mapper, ref message, ref code)).WithMessage(x => message).WithErrorCode(code.ToString());
        }

        public static IRuleBuilderOptions<T, TProperty> WithErrorCode<T, TProperty>
            (this IRuleBuilderOptions<T, TProperty> rule, Func<T, string> errorProvider)
        {
            DefaultValidatorOptions.Configurable(rule).Current.ErrorCode = errorProvider.ToString();
            return rule;
        }
    }
}
