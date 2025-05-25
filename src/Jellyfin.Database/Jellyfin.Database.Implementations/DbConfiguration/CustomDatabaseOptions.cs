using System.Collections.Generic;

namespace Jellyfin.Database.Implementations.DbConfiguration;

/// <summary>
/// Defines the options for a custom database connector.
/// </summary>
public class CustomDatabaseOptions
{
    /// <summary>
    /// Gets or sets the Plugin name to search for database providers.
    /// </summary>
    public required string PluginName { get; set; }

    /// <summary>
    /// Gets or sets the plugin assembly to search for providers.
    /// </summary>
    public required string PluginAssembly { get; set; }

    /// <summary>
    /// Gets or sets the connection string for the custom database provider.
    /// </summary>
    public required string ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the list of extra options for the custom provider.
    /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
    public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
#pragma warning restore CA2227 // Collection properties should be read only
}
