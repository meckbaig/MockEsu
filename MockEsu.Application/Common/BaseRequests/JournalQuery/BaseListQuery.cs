using MockEsu.Application.Common.Attributes;
using MockEsu.Application.Extensions.JournalFilters;
using MockEsu.Application.Extensions.ListFilters;
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
        public string? orderBy { get; set; }

        private readonly Dictionary<FilterExpression, FilterableAttribute> _filterExpressions = [];
        private readonly List<OrderByExpression> _orderExpressions = [];
        public Dictionary<FilterExpression, FilterableAttribute> GetFilterExpressions() => _filterExpressions;
        public List<OrderByExpression> GetOrderExpressions() => _orderExpressions;
        public void AddFilterExpression(FilterExpression expression, FilterableAttribute attribute)
            => _filterExpressions!.Add(expression, attribute);
        public void AddOrderExpression(OrderByExpression expression)
            => _orderExpressions!.Add(expression);
    }
}
