using MockEsu.Application.Common.Attributes;
using MockEsu.Application.Extensions.ListFilters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Application.Common.BaseRequests.JournalQuery
{
    public abstract class BaseListQueryResponse<TResult> : BaseResponse where TResult : BaseDto
    {
        public abstract IList<TResult> Items { get; set; }
    }
}
