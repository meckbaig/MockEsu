using Microsoft.AspNetCore.Authorization;
using MockEsu.Infrastructure.Authentification;

namespace MockEsu.Infratructure.Authentification;

public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(Permission permission) : base(policy: permission.ToString())
    {
        
    }
}
