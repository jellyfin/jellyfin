using System;

namespace Jellyfin.Data.Attributes;

/// <summary>
/// Attribute to specify that the enum value is to be ignored when generating the openapi spec.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class OpenApiIgnoreEnumAttribute : Attribute
{
}
