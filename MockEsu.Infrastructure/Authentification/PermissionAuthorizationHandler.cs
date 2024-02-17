using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;

namespace MockEsu.Infrastructure.Authentification;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        HashSet<string> permissions = context.User.Claims
            .Where(x => x.Type == "permissions")
            .Select(selector => selector.Value)
            .ToHashSet();
        if (permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}

public interface IPermissionService
{
    Task<HashSet<string>> GetPermissionsAsync();
}