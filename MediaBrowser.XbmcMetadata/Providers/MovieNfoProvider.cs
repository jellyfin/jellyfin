using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Providers
{
    /// <summary>
    /// Nfo provider for movies.
    /// </summary>
    public class MovieNfoProvider : BaseVideoNfoProvider<Movie>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MovieNfoProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="config">the configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        public MovieNfoProvider(
            ILogger<MovieNfoProvider> logger,
            IFileSystem fileSystem,
            IConfigurationManager config,
            IProviderManager providerManager)
            : base(logger, fileSystem, config, providerManager)
        {
        }
    }
}
