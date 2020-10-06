using Microsoft.Extensions.Configuration;

namespace MediaBrowser.Providers.Plugins.Tmdb
{
    /// <summary>
    /// Configuration extensions for tmdb.
    /// </summary>
    public static class TmdbConfigurationExtensions
    {
        /// <summary>
        /// The key for the Tmdb api key.
        /// </summary>
        public const string TmdbApiKeyKey = "tmdb:apiKey";

        /// <summary>
        /// Gets the tmdb api key.
        /// </summary>
        /// <param name="configuration">The configuration to read the setting from.</param>
        /// <returns>The tmdb api key.</returns>
        public static string GetTmdbApiKey(this IConfiguration configuration)
            => configuration[TmdbApiKeyKey];
    }
}
