using System;

namespace Jellyfin.Server.Implementations.DatabaseConfiguration;

/// <summary>
/// Options specific to run jellyfin on a postgreSql database.
/// </summary>
public class PostgreSqlOptions
{
    /// <summary>
    /// Gets or Sets the Port. Defaults to 5432.
    /// </summary>
    public required int Port { get; set; } = 5432;

    /// <summary>
    /// Gets or Sets the Server name.
    /// </summary>
    public required string ServerName { get; set; }

    /// <summary>
    /// Gets or Sets the username.
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// Gets or Sets the password.
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// Gets or Sets the database name. Defaults to "Jellyfin".
    /// </summary>
    public string DatabaseName { get; set; } = "Jellyfin";

    /// <summary>
    /// Gets or Sets the timeout in secounds before a running command is terminated. Defaults to 30.
    /// </summary>
    public int Timeout { get; set; } = 30;
}
