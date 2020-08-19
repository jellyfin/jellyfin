using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.DownloadPolicy
{
    /// <summary>
    /// The download permission requirement.
    /// </summary>
    public class DownloadRequirement : IAuthorizationRequirement
    {
    }
}
