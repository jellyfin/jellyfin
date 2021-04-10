using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.NetworkAccessPolicy
{
    /// <summary>
    /// The local network authorization requirement.
    /// </summary>
    public class NetworkAccessRequirement : IAuthorizationRequirement
    {
    }
}
