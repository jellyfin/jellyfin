#pragma warning disable CA1813 // Avoid unsealed attributes

using System;

namespace Jellyfin.Api.Attributes;

/// <summary>
/// Internal produces image attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class AcceptsFileAttribute : Attribute
{
    private readonly string[] _contentTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptsFileAttribute"/> class.
    /// </summary>
    /// <param name="contentTypes">Content types this endpoint produces.</param>
    public AcceptsFileAttribute(params string[] contentTypes)
    {
        _contentTypes = contentTypes;
    }

    /// <summary>
    /// Gets the configured content types.
    /// </summary>
    /// <returns>the configured content types.</returns>
    public string[] ContentTypes => _contentTypes;
}
