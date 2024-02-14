using Microsoft.AspNetCore.JsonPatch;
using MockEsu.Application.DTOs.Tariffs;
using MockEsu.Application.Services.Kontragents;
using MockEsu.Application.Services.Tariffs;

namespace MockEsu.Web.Controllers.V1
{
    [Route("api/v{version:ApiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1")]
    public class TariffsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TariffsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPatch]
        public async Task<ActionResult<JsonPatchTariffsResponse>> UpdateList(
            [FromBody] JsonPatchDocument<TariffEditDto> items)
        {
            JsonPatchTariffsCommand command = new() { Patch = items };
            var result = await _mediator.Send(command);
            return result.ToJsonResponse();
        }

        [HttpGet]
        [Route("Get")]
        public async Task<ActionResult<GetTariffsResponse>> GetList([FromQuery] GetTariffsQuery query)
        {
            var result = await _mediator.Send(query);
            return result.ToJsonResponse();
        }
    }
}
