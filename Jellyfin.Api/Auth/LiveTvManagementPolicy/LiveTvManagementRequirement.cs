using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.LiveTvManagementPolicy;

/// <summary>
/// The LiveTV management requirement.
/// </summary>
public class LiveTvManagementRequirement : IAuthorizationRequirement
{
}
