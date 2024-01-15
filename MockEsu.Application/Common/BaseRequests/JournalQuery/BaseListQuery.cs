using MockEsu.Application.Extensions.ListFilters;
using System.Linq.Expressions;

namespace MockEsu.Application.Common.BaseRequests.JournalQuery
{
    public record BaseListQuery<TResponse> : BaseRequest<TResponse>
        where TResponse : BaseResponse
    {
        public int skip { get; set; }
        public int take { get; set; } = int.MaxValue;
        public string[]? filters { get; set; }
        public string[]? orderBy { get; set; }

        private readonly List<Expression> _filterExpressions = [];
        private readonly List<OrderByExpression> _orderExpressions = [];
        public List<Expression> GetFilterExpressions() => _filterExpressions;
        public List<OrderByExpression> GetOrderExpressions() => _orderExpressions;
        public void AddFilterExpression(Expression expression)
            => _filterExpressions!.Add(expression);
        public void AddOrderExpression(OrderByExpression expression)
            => _orderExpressions!.Add(expression);
    }
}
