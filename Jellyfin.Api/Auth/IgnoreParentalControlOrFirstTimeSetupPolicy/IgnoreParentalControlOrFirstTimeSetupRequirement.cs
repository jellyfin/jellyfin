using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.IgnoreParentalControlOrFirstTimeSetupPolicy
{
    /// <summary>
    /// Escape schedule controls requirement.
    /// </summary>
    public class IgnoreParentalControlOrFirstTimeSetupRequirement : IAuthorizationRequirement
    {
    }
}
