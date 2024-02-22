using Microsoft.AspNetCore.JsonPatch;
using MockEsu.Application.Common.Dtos;

namespace MockEsu.Application.Common.BaseRequests.JsonPatchCommand;

public abstract record BaseJsonPatchCommand<TResponse, TDto> : BaseRequest<TResponse>
    where TResponse : BaseResponse
    where TDto : class, IBaseDto, IEditDto, new()
{
    public abstract JsonPatchDocument<TDto> Patch { get; set; }
}
