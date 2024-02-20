using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.BaseRequests.JsonPatchCommand;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.DTOs.Roles;
using MockEsu.Application.DTOs.Tariffs;
using MockEsu.Application.Extensions.JsonPatch;
using MockEsu.Domain.Entities.Authentification;

namespace MockEsu.Application.Services.Roles;

public record JsonPatchRolesCommand : BaseJsonPatchCommand<JsonPatchRolesResponse, RoleEditDto>
{
    public override JsonPatchDocument<RoleEditDto> Patch { get; set; }
}

public class JsonPatchRolesResponse : BaseResponse
{
    public List<RolePreviewDto> Roles { get; set; }
}

public class JsonPatchRolesCommandValidator : BaseJsonPatchValidator
    <JsonPatchRolesCommand, JsonPatchRolesResponse, RoleEditDto>
{
    public JsonPatchRolesCommandValidator(IMapper mapper) : base(mapper)
    {
    }
}

public class JsonPatchRolesCommandHandler : IRequestHandler<JsonPatchRolesCommand, JsonPatchRolesResponse>
{
    private readonly IAppDbContext _context;
    private readonly IMapper _mapper;

    public JsonPatchRolesCommandHandler(IAppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<JsonPatchRolesResponse> Handle(JsonPatchRolesCommand request, CancellationToken cancellationToken)
    {
        //Role role = new Role { Id = 4 };
        //Permission permission = 2;
        //_context.Entry(role).State = EntityState.Unchanged;
        //_context.Entry(permission).State = EntityState.Unchanged;
        //role.Permissions.Add(permission);
        //_context.SaveChanges();

        request.Patch.ApplyDtoTransactionToSource(_context.Roles, _mapper.ConfigurationProvider);

        var roles = _context.Roles.AsNoTracking()
            .Include(r => r.Permissions)
            .Select(r => _mapper.Map<RolePreviewDto>(r))
            .ToList();

        return new JsonPatchRolesResponse { Roles = roles };
    }
}
