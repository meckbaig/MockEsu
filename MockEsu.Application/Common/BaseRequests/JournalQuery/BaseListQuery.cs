using System.Collections;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Runtime.InteropServices.JavaScript;
using MockEsu.Application.Extensions.ListFilters;

namespace MockEsu.Application.Common.BaseRequests.JournalQuery;

public record BaseListQuery<TResponse> : BaseRequest<TResponse>
    where TResponse : BaseResponse
{
    public int skip { get; set; }
    public int take { get; set; } = int.MaxValue;
    public string[]? filters { get; set; }
    public string[]? orderBy { get; set; }

    private readonly List<Expression> _filterExpressions = [];
    private readonly List<OrderByExpression> _orderExpressions = [];
    private string _key;
    
    public List<Expression> GetFilterExpressions() 
        => _filterExpressions;
    
    public List<OrderByExpression> GetOrderExpressions() 
        => _orderExpressions;

    public void AddFilterExpression(Expression expression)
        => _filterExpressions!.Add(expression);

    public void AddOrderExpression(OrderByExpression expression)
        => _orderExpressions!.Add(expression);

    public string GetKey()
    {
        if (_key is null)
        {
            Dictionary<string, string> props = new();
            foreach (var prop in GetType().GetProperties())
            {
                var value = prop.GetValue(this, null);
                if (value != null)
                {
                    if (value is IEnumerable)
                        props.Add(prop.Name, string.Join(',', (value as IEnumerable).ToDynamicList()));
                    else
                        props.Add(prop.Name, value.ToString());
                        
                }
            }
            _key = $"{GetType().Name}-{string.Join(';', props)}";
        }
        return _key;
    }
}