using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.LiveTvAccessPolicy;

/// <summary>
/// The LiveTV access requirement.
/// </summary>
public class LiveTvAccessRequirement : IAuthorizationRequirement
{
}
