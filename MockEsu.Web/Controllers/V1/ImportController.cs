using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MockEsu.Application.Services.Data;
using System.Text;
using System.Text.Json;

namespace MockEsu.Web.Controllers.V1
{
    [Route("api/v{version:ApiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1")]
    public class ImportController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ImportController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<ActionResult<ImportKontragentsFromKsuResponse>> Index([FromForm] ImportKontragentsFromKsuCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.result)
                return Ok(result);
            else 
                return BadRequest();
        }
    }
}
