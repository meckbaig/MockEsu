using MediatR;

namespace MockEsu.Application.Common.BaseRequests
{
    public record BaseRequest<TResponse> : IRequest<TResponse> where TResponse : BaseResponse { }
}
