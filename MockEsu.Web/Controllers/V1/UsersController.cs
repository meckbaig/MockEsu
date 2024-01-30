using Microsoft.AspNetCore.JsonPatch;
using MockEsu.Application.DTOs.Users;
using MockEsu.Application.Services.Users;

namespace MockEsu.Web.Controllers.V1;

[Route("api/v{version:ApiVersion}/[controller]")]
[ApiController]
[ApiVersion("1")]
public class UsersController: ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<GetUsersResponse>> GetList([FromQuery] GetUsersQuery query)
    {
        var result = await _mediator.Send(query);
        return result.ToJsonResponse();
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<ActionResult<GetUserByIdResponse>> GetById(int id)
    {
        GetUserByIdQuery query = new() { id = id };
        var result = await _mediator.Send(query);
        return result.ToJsonResponse();
    }


    [HttpPost]
    public async Task<ActionResult<CreateUserResponse>> Create([FromBody] CreateUserCommand command)
    {
        var result = await _mediator.Send(command);
        return result.ToJsonResponse();
    }

    [HttpPatch]
    [Route("{id}")]
    public async Task<ActionResult<UpdateUserResponse>> Update(
        int id,
        [FromBody] JsonPatchDocument<UserEditDto> items)
    {
        UpdateUserCommand command = new() { Id = id, Patch = items };
        var result = await _mediator.Send(command);
        return result.ToJsonResponse();
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task<ActionResult<DeleteUserResponse>> Delete(int id)
    {
        var command = new DeleteUserCommand { id = id };
        var result = await _mediator.Send(command);
        return result.ToJsonResponse();
    }
}