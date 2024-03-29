﻿using Microsoft.AspNetCore.JsonPatch;
using MockEsu.Application.DTOs.Kontragents;
using MockEsu.Application.Services.Kontragents;
using MockEsu.Domain.Enums;
using MockEsu.Infratructure.Authentification;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace MockEsu.Web.Controllers.V1;

[Route("api/v{version:ApiVersion}/[controller]")]
[ApiController]
[ApiVersion("1")]
public class KontragentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public KontragentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Route("Hello")]
    public async Task<InitSqlResponse> GetList([FromQuery] InitSqlQuery query)
    {
        return await _mediator.Send(query);
    }

    [HttpGet]
    [HasPermission(Permission.ReadMember)]
    [Route("Get")]
    public async Task<ActionResult<GetKontragentsResponse>> GetList([FromQuery] GetKontragentsQuery query)
    {
        var result = await _mediator.Send(query);
        return result.ToJsonResponse();
    }

    [HttpPatch]
    public async Task<ActionResult<JsonPatchKontragentsResponse>> UpdateList(
        [FromBody] JsonPatchDocument<KontragentEditDto> items)
    {
        JsonPatchKontragentsCommand command = new() { Patch = items };
        var result = await _mediator.Send(command);
        return result.ToJsonResponse();
    }
}