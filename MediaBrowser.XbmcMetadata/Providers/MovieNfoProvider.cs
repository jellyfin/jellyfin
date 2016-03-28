using CommonIO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.XbmcMetadata.Providers
{
    public class MovieNfoProvider : BaseVideoNfoProvider<Movie>
    {
        public MovieNfoProvider(IFileSystem fileSystem, ILogger logger, IConfigurationManager config) : base(fileSystem, logger, config)
        {
        }
    }

    public class MusicVideoNfoProvider : BaseVideoNfoProvider<MusicVideo>
    {
        public MusicVideoNfoProvider(IFileSystem fileSystem, ILogger logger, IConfigurationManager config) : base(fileSystem, logger, config)
        {
        }
    }

    public class VideoNfoProvider : BaseVideoNfoProvider<Video>
    {
        public VideoNfoProvider(IFileSystem fileSystem, ILogger logger, IConfigurationManager config) : base(fileSystem, logger, config)
        {
        }
    }
}