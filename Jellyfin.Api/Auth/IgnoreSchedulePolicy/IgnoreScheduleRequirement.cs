using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.IgnoreSchedulePolicy
{
    /// <summary>
    /// Escape schedule controls requirement.
    /// </summary>
    public class IgnoreScheduleRequirement : IAuthorizationRequirement
    {
    }
}
