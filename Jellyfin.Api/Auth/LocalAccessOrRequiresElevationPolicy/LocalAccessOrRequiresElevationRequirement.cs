using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.LocalAccessOrRequiresElevationPolicy
{
    /// <summary>
    /// The local access authorization requirement.
    /// </summary>
    public class LocalAccessOrRequiresElevationRequirement : IAuthorizationRequirement
    {
    }
}
