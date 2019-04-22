using Jellyfin.Common.Configuration;
using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Entities.Movies;
using Jellyfin.Controller.Providers;
using Jellyfin.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.XbmcMetadata.Providers
{
    public class MovieNfoProvider : BaseVideoNfoProvider<Movie>
    {
        public MovieNfoProvider(IFileSystem fileSystem, ILogger logger, IConfigurationManager config, IProviderManager providerManager)
            : base(fileSystem, logger, config, providerManager)
        {
        }
    }

    public class MusicVideoNfoProvider : BaseVideoNfoProvider<MusicVideo>
    {
        public MusicVideoNfoProvider(IFileSystem fileSystem, ILogger logger, IConfigurationManager config, IProviderManager providerManager)
            : base(fileSystem, logger, config, providerManager)
        {
        }
    }

    public class VideoNfoProvider : BaseVideoNfoProvider<Video>
    {
        public VideoNfoProvider(IFileSystem fileSystem, ILogger logger, IConfigurationManager config, IProviderManager providerManager)
            : base(fileSystem, logger, config, providerManager)
        {
        }
    }
}
