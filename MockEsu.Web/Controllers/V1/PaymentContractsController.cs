using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MockEsu.Application.Services.PaymentContracts;

namespace MockEsu.Web.Controllers.V1;

[Route("api/v{version:ApiVersion}/[controller]")]
[ApiController]
[ApiVersion("1")]
[Authorize]
public class PaymentContractsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentContractsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<GetPaymentContractsResponse>> GetList([FromQuery] GetPaymentContractsQuery query)
    {
        var result = await _mediator.Send(query);
        return result.ToJsonResponse();
    }
    
    [HttpGet]
    [Route("Admin")]
    [Authorize(Roles = "admin, boss")]
    public async Task<ActionResult<GetPaymentContractsResponse>> GetListOnlyAdmin([FromQuery] GetPaymentContractsQuery query)
    {
        query.take = 1;
        var result = await _mediator.Send(query);
        return result.ToJsonResponse();
    }
}