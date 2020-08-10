using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.FirstTimeOrIgnoreParentalControlSetupPolicy
{
    /// <summary>
    /// First time setup or ignore parental controls requirement.
    /// </summary>
    public class FirstTimeOrIgnoreParentalControlSetupRequirement : IAuthorizationRequirement
    {
    }
}
