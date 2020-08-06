using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.FirstTimeSetupOrDefaultPolicy
{
    /// <summary>
    /// The authorization requirement, requiring incomplete first time setup or elevated privileges, for the authorization handler.
    /// </summary>
    public class FirstTimeSetupOrDefaultRequirement : IAuthorizationRequirement
    {
    }
}
