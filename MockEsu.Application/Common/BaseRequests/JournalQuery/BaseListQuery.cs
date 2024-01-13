using MockEsu.Application.Common.Attributes;
using MockEsu.Application.Extensions.JournalFilters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Application.Common.BaseRequests.JournalQuery
{
    public record BaseListQuery<TResponse> : BaseRequest<TResponse>
        where TResponse : BaseResponse
    {
        public int skip { get; set; }
        public int take { get; set; } = int.MaxValue;
        public string[]? filters { get; set; }

        private readonly Dictionary<FilterExpression, FilterableAttribute>? _filterExpressions = [];
        public Dictionary<FilterExpression, FilterableAttribute>? GetFilterExpressions() => _filterExpressions;
        public void AddFilterExpression(FilterExpression expression, FilterableAttribute attribute)
            => _filterExpressions!.Add(expression, attribute);
    }
}
