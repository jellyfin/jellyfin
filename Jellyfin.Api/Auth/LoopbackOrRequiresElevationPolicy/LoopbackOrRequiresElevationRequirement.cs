using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.LoopbackOrRequiresElevationPolicy
{
    /// <summary>
    /// The loopback access or elevated privileges authorization requirement.
    /// </summary>
    public class LoopbackOrRequiresElevationRequirement : IAuthorizationRequirement
    {
    }
}
