using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Application.Common.BaseRequests.JournalQuery
{
    public record BaseJournalQuery<TResponse> : BaseRequest<TResponse>
        where TResponse : BaseResponse
    {
        public int skip { get; set; }
        public int take { get; set; } = int.MaxValue;
        public string[]? filters { get; set; }

        private List<string>? _filterExpressions = [];
        public List<string>? GetFilterExpressions() => _filterExpressions;
        public void AddFilterExpression(string expression)
            => _filterExpressions.Add(expression);
    }
}
