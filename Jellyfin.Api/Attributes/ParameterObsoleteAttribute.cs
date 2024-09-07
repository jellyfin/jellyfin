using System;

namespace Jellyfin.Api.Attributes;

/// <summary>
/// Attribute to mark a parameter as obsolete.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ParameterObsoleteAttribute : Attribute
{
}
