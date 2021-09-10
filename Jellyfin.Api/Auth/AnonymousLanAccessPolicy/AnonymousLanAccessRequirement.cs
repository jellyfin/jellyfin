using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.AnonymousLanAccessPolicy
{
    /// <summary>
    /// The local network authorization requirement. Allows anonymous users.
    /// </summary>
    public class AnonymousLanAccessRequirement : IAuthorizationRequirement
    {
    }
}
