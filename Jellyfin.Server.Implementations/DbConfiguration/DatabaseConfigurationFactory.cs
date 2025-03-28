using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;

namespace Jellyfin.Server.Implementations.DatabaseConfiguration;

/// <summary>
/// Factory for constructing a database configuration.
/// </summary>
public class DatabaseConfigurationFactory : IConfigurationFactory
{
    /// <inheritdoc/>
    public IEnumerable<ConfigurationStore> GetConfigurations()
    {
        yield return new DatabaseConfigurationStore();
    }
}
