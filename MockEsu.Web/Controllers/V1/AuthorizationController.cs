using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using MockEsu.Application.Services.Authorization;

namespace MockEsu.Web.Controllers.V1;

[Route("api/v{version:ApiVersion}/[controller]")]
[ApiController]
[ApiVersion("1")]
public class AuthorizationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly EndpointDataSource _endpointDataSource;

    public AuthorizationController(IMediator mediator, EndpointDataSource endpointDataSource)
    {
        _mediator = mediator;
        _endpointDataSource = endpointDataSource;
    }

    [HttpGet]
    [ApiVersion("1.1")]
    [ApiVersion("1.2")]
    public async Task<ActionResult<AuthorizeUserResponse>> GetList([FromQuery] AuthorizeUserQuery query)
    {
        var result = await _mediator.Send(query);
        return result.ToJsonResponse();
    }

    [HttpGet]
    [Route("GetEndpoints")]
    public Task<List<EndpointInfo>> GetEndpoints()
    {
        var endpoints = new List<EndpointInfo>();
        foreach (var endpoint in _endpointDataSource.Endpoints)
        {
            string routePattern = (endpoint as RouteEndpoint)?.RoutePattern?.RawText ?? "N/A";
            var roles = new List<string>();
            bool requresAuth = false;
            foreach (var attribute in endpoint.Metadata)
            {
                if (attribute is AuthorizeAttribute)
                {
                    requresAuth = true;
                    if ((attribute as AuthorizeAttribute).Roles != null)
                        roles.AddRange((attribute as AuthorizeAttribute).Roles.Split(',').Select(r => r.Trim()));
                }
            }
            var apiVersionProviders = endpoint.Metadata.OfType<IApiVersionProvider>();
            List<string> versions = apiVersionProviders.SelectMany(p => p.Versions)?.Select(v => v.ToString()).ToList();
            if (versions.FirstOrDefault(v => v.Contains('.')) != null)
                versions = versions.Except(versions.Where(v => !v.Contains('.'))).ToList();

            endpoints.Add(new EndpointInfo
            {
                RequresAuth = requresAuth,
                AllowedRoles = roles,
                RoutePattern = routePattern.Replace("{version:ApiVersion}", $"{{{string.Join(',', versions)}}}"),
            });
        }

        return Task.FromResult(endpoints);
    }


    public class EndpointInfo
    {
        public string EndpointName { get; set; }
        public bool RequresAuth { get; set; }
        public List<string> AllowedRoles { get; set; }
        public string RoutePattern { get; set; }

        public override string ToString() => RoutePattern;
    }
}