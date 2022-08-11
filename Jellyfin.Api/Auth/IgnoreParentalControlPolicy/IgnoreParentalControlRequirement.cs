using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.IgnoreParentalControlPolicy
{
    /// <summary>
    /// Escape schedule controls requirement.
    /// </summary>
    public class IgnoreParentalControlRequirement : IAuthorizationRequirement
    {
    }
}
