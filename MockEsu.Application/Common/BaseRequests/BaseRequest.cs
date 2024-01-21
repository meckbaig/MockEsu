using System.Collections;
using System.Linq.Dynamic.Core;
using MediatR;

namespace MockEsu.Application.Common.BaseRequests;

public record BaseRequest<TResponse> : IRequest<TResponse> where TResponse : BaseResponse
{
        
    private string? _key = null;
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
                    if (value is IEnumerable enumerable)
                        props.Add(prop.Name, string.Join(',', enumerable.ToDynamicList()));
                    else
                        props.Add(prop.Name, value.ToString()!);
                        
                }
            }
            _key = $"{GetType().Name}-{string.Join(';', props)}";
        }
        return _key;
    }
}