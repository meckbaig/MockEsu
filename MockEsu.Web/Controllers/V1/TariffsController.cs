using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using MockEsu.Application.DTOs.Tariffs;
using MockEsu.Application.DTOs.Users;
using MockEsu.Application.Services.Tariffs;
using MockEsu.Application.Services.Users;

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
        [Route("{id}")]
        public async Task<ActionResult<JsonPatchTariffResponse>> Update(
            int id,
            [FromBody] JsonPatchDocument<TariffDto> items)
        {
            JsonPatchTariffCommand command = new() { Id = id, Patch = items };
            var result = await _mediator.Send(command);
            return result.ToJsonResponse();
        }

        //[HttpPatch]
        //[Route("{id}/prices/{priceId}")]
        //public async Task<ActionResult<JsonPatchPricesResponse>> Update(
        //    int id, string priceId,
        //    [FromBody] JsonPatchDocument<TariffPriceEditDto> items)
        //{
        //    var pair = new JsonPatchPair<TariffPriceEditDto>(priceId, [items]);
        //    JsonPatchPricesCommand command = new() { Id = id, Patch = [pair] };
        //    var result = await _mediator.Send(command);
        //    return result.ToJsonResponse();
        //}
    }
}
