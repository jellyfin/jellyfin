using System.Collections.Generic;

namespace Jellyfin.Database.Implementations.DbConfiguration;

/// <summary>
/// Options to configure jellyfins managed database.
/// </summary>
public class DatabaseConfigurationOptions
{
    /// <summary>
    /// Gets or Sets the type of database jellyfin should use.
    /// </summary>
    public required string DatabaseType { get; set; }

    /// <summary>
    /// Gets or sets the options required to use a custom database provider.
    /// </summary>
    public CustomDatabaseOptions? CustomProviderOptions { get; set; }
}
