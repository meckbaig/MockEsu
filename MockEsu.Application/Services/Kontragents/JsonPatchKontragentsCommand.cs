using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.BaseRequests.JsonPatchCommand;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.DTOs.Kontragents;
using MockEsu.Application.Extensions.JsonPatch;
using MockEsu.Application.Services.Tariffs;

namespace MockEsu.Application.Services.Kontragents;

public record JsonPatchKontragentsCommand : BaseJsonPatchCommand<JsonPatchKontragentsResponse, KontragentEditDto>
{
    public override JsonPatchDocument<KontragentEditDto> Patch { get; set; }
}

public class JsonPatchKontragentsResponse : BaseResponse
{

}

public class JsonPatchKontragentsCommandValidator : BaseJsonPatchValidator
    <JsonPatchKontragentsCommand, JsonPatchKontragentsResponse, KontragentEditDto>
{
    public JsonPatchKontragentsCommandValidator(IMapper mapper) : base(mapper) { }
}

public class JsonPatchKontragentsCommandHandler : IRequestHandler<JsonPatchKontragentsCommand, JsonPatchKontragentsResponse>
{
    private readonly IAppDbContext _context;
    private readonly IMapper _mapper;

    public JsonPatchKontragentsCommandHandler(IAppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<JsonPatchKontragentsResponse> Handle(JsonPatchKontragentsCommand request, CancellationToken cancellationToken)
    {
        request.Patch.ApplyDtoTransactionToSource(_context.Kontragents, _mapper.ConfigurationProvider);
        return new JsonPatchKontragentsResponse();
    }
}
