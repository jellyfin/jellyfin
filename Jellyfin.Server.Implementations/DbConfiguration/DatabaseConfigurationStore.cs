using System;
using System.Collections.Generic;
using Jellyfin.Database.Implementations.DbConfiguration;
using MediaBrowser.Common.Configuration;

namespace Jellyfin.Server.Implementations.DatabaseConfiguration;

/// <summary>
/// A configuration that stores database related settings.
/// </summary>
public class DatabaseConfigurationStore : ConfigurationStore
{
    /// <summary>
    /// The name of the configuration in the storage.
    /// </summary>
    public const string StoreKey = "database";

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseConfigurationStore"/> class.
    /// </summary>
    public DatabaseConfigurationStore()
    {
        ConfigurationType = typeof(DatabaseConfigurationOptions);
        Key = StoreKey;
    }
}
