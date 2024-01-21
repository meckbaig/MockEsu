namespace MockEsu.Application.Common.BaseRequests.JournalQuery
{
    public abstract class BaseListQueryResponse<TResult> : BaseResponse where TResult : BaseDto
    {
        public abstract IList<TResult> Items { get; set; }
    }
}