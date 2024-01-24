using Microsoft.AspNetCore.Mvc;
using MockEsu.Application.Services.Authorization;

namespace MockEsu.Web.Controllers.V1;

[Route("api/v{version:ApiVersion}/[controller]")]
[ApiController]
[ApiVersion("1")]
public class AuthorizationController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthorizationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<AuthorizeUserResponse>> GetList([FromQuery] AuthorizeUserQuery query)
    {
        var result = await _mediator.Send(query);
        return result.ToJsonResponse();
    }
}