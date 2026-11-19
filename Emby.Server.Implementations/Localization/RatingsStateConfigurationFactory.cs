using System.Collections.Generic;
using MediaBrowser.Common.Configuration;

namespace Emby.Server.Implementations.Localization;

/// <summary>
/// A configuration factory for <see cref="RatingsStateData"/>.
/// </summary>
public class RatingsStateConfigurationFactory : IConfigurationFactory
{
    /// <summary>
    /// The key of the ratings state configuration.
    /// </summary>
    public const string Key = "ratingsstate";

    /// <inheritdoc />
    public IEnumerable<ConfigurationStore> GetConfigurations()
    {
        yield return new ConfigurationStore
        {
            ConfigurationType = typeof(RatingsStateData),
            Key = Key
        };
    }
}
