using Jellyfin.Api.Auth.DefaultAuthorizationPolicy;

namespace Jellyfin.Api.Auth;

/// <summary>
/// Allows a normal account credential or a resource-scoped playback credential.
/// Both remain subject to the default user and network checks.
/// </summary>
public sealed class StreamingAuthorizationRequirement : DefaultAuthorizationRequirement
{
}
