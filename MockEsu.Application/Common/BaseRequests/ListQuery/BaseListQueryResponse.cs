using MockEsu.Application.Common.Dtos;

namespace MockEsu.Application.Common.BaseRequests.ListQuery
{
    public abstract class BaseListQueryResponse<TResult> : BaseResponse where TResult : IBaseDto
    {
        public virtual IList<TResult> Items { get; set; }
    }
}