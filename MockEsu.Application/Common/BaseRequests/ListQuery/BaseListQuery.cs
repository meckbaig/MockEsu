using System.Collections;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Runtime.InteropServices.JavaScript;
using MockEsu.Application.Extensions.ListFilters;

namespace MockEsu.Application.Common.BaseRequests.ListQuery;

public abstract record BaseListQuery<TResponse> : BaseRequest<TResponse>
    where TResponse : BaseResponse
{
    // ReSharper disable InconsistentNaming
    public abstract int skip { get; set; }
    public abstract int take { get; set; }
    public abstract string[]? filters { get; set; }
    public abstract string[]? orderBy { get; set; }
    // ReSharper restore InconsistentNaming

    private readonly List<Expression> _filterExpressions = [];
    private readonly List<OrderByExpression> _orderExpressions = [];
    
    public List<Expression> GetFilterExpressions() 
        => _filterExpressions;
    
    public List<OrderByExpression> GetOrderExpressions() 
        => _orderExpressions;

    public void AddFilterExpression(Expression expression)
        => _filterExpressions!.Add(expression);

    public void AddOrderExpression(OrderByExpression expression)
        => _orderExpressions!.Add(expression);
}