using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.LocalAccessPolicy
{
    /// <summary>
    /// The local access authorization requirement.
    /// </summary>
    public class LocalAccessRequirement : IAuthorizationRequirement
    {
    }
}
