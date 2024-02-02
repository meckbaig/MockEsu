using Microsoft.AspNetCore.Mvc;
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

    [HttpPost]
    public async Task<ActionResult<GetUserByIdResponse>> Create([FromBody] CreateRoleCommand command)
    {
        var result = await _mediator.Send(command);
        return result.ToJsonResponse();
    }
}
