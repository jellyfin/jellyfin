using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.PlaylistShareAccessPolicy
{
    /// <summary>
    /// The playlist share access authorization requirement. Allows anonymous users with valid share token.
    /// </summary>
    public class PlaylistShareAccessRequirement : IAuthorizationRequirement
    {
    }
}
