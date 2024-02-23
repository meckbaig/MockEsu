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
    public virtual int skip { get; set; }
    public virtual int take { get; set; }
    public virtual string[]? filters { get; set; }
    public virtual string[]? orderBy { get; set; }
    // ReSharper restore InconsistentNaming

    private readonly Dictionary<ParameterExpression, Expression> _filterExpressions = [];
    private readonly List<OrderByExpression> _orderExpressions = [];
    
    public Dictionary<ParameterExpression, Expression> GetFilterExpressions() 
        => _filterExpressions;
    
    public List<OrderByExpression> GetOrderExpressions() 
        => _orderExpressions;

    public void AddFilterExpression(ParameterExpression param, Expression expression)
        => _filterExpressions!.Add(param, expression);

    public void AddOrderExpression(OrderByExpression expression)
        => _orderExpressions!.Add(expression);
}