using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.FirstTimeSetupOrElevatedPolicy
{
    /// <summary>
    /// The authorization requirement, requiring incomplete first time setup or elevated privileges, for the authorization handler.
    /// </summary>
    public class FirstTimeSetupOrElevatedRequirement : IAuthorizationRequirement
    {
    }
}
