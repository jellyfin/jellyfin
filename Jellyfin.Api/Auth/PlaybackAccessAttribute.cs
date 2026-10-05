using System;

namespace Jellyfin.Api.Auth;

/// <summary>
/// Marks media resource endpoints that accept scoped playback credentials.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PlaybackAccessAttribute : Attribute
{
}
