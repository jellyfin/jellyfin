using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.LocalOrElevatedPolicy
{
    /// <summary>
    /// The authorization requirement, requiring a request from the local computer or elevated privileges, for the authorization handler.
    /// </summary>
    public class LocalOrElevatedPolicyRequirement : IAuthorizationRequirement
    {
    }
}
