using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.ShareLinkPolicy;

/// <summary>
/// Requirement for scoped share link access.
/// </summary>
public class ShareLinkRequirement : IAuthorizationRequirement
{
}
