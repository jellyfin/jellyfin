using System;

namespace Jellyfin.Api.Attributes;

/// <summary>
/// Marks an endpoint as accessible with a scoped share link token.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class ShareLinkAccessAttribute : Attribute
{
}
