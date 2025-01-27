using System;

namespace Jellyfin.Server.Implementations.DatabaseConfiguration;

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
    /// Gets or Sets the settings to run jellyfin with Postgres.
    /// </summary>
    public PostgreSqlOptions? PostgreSql { get; set; }
}
