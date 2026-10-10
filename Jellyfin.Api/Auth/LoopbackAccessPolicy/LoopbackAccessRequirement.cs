using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.LoopbackAccessPolicy
{
    /// <summary>
    /// The loopback access authorization requirement.
    /// </summary>
    public class LoopbackAccessRequirement : IAuthorizationRequirement
    {
    }
}
