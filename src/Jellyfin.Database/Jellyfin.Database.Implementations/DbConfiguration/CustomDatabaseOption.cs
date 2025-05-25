using System.Collections.Generic;

namespace Jellyfin.Database.Implementations.DbConfiguration;

/// <summary>
/// The custom value option for custom database providers.
/// </summary>
public class CustomDatabaseOption
{
    /// <summary>
    /// Gets or sets the key of the value.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public required string Value { get; set; }
}
