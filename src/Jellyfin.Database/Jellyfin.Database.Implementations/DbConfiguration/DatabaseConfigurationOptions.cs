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
}
