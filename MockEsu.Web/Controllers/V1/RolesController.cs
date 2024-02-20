using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using MockEsu.Application.DTOs.Roles;
using MockEsu.Application.Services.Roles;
using MockEsu.Application.Services.Users;

namespace MockEsu.Web.Controllers.V1;

[Route("api/v{version:ApiVersion}/[controller]")]
[ApiController]
[ApiVersion("1")]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPatch]
    public async Task<ActionResult<JsonPatchRolesResponse>> Patch([FromBody] JsonPatchDocument<RoleEditDto> patch)
    {
        JsonPatchRolesCommand command = new() { Patch = patch };
        var result = await _mediator.Send(command);
        return result.ToJsonResponse();
    }
}
