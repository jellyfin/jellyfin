using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.LocalNetworkAccessPolicy
{
    /// <summary>
    /// The local network authorization requirement.
    /// </summary>
    public class LocalNetworkAccessRequirement : IAuthorizationRequirement
    {
    }
}
