using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.UserSessionsPolicy
{
    /// <summary>
    /// Limit the number of user sessions requirement.
    /// </summary>
    public class UserSessionsRequirement : IAuthorizationRequirement
    {
    }
}
