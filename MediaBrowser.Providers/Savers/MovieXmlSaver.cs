using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Movies;
using System;
using System.IO;
using System.Text;
using System.Threading;

namespace MediaBrowser.Providers.Savers
{
    /// <summary>
    /// Saves movie.xml for movies, trailers and music videos
    /// </summary>
    public class MovieXmlSaver : IMetadataSaver
    {
        private readonly IServerConfigurationManager _config;

        public MovieXmlSaver(IServerConfigurationManager config)
        {
            _config = config;
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool Supports(BaseItem item)
        {
            if (!_config.Configuration.SaveLocalMeta || item.LocationType != LocationType.FileSystem)
            {
                return false;
            }

            var trailer = item as Trailer;

            if (trailer != null)
            {
                return !trailer.IsLocalTrailer;
            }

            // Don't support local trailers
            return item is Movie || item is MusicVideo;
        }

        /// <summary>
        /// Saves the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public void Save(BaseItem item, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();

            builder.Append("<Title>");

            XmlSaverHelpers.AddCommonNodes(item, builder);
            XmlSaverHelpers.AppendMediaInfo((Video)item, builder);

            builder.Append("</Title>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new string[] { });

            // Set last refreshed so that the provider doesn't trigger after the file save
            MovieProviderFromXml.Current.SetLastRefreshed(item, DateTime.UtcNow);
        }

        public string GetSavePath(BaseItem item)
        {
            var video = (Video)item;

            var directory = video.VideoType == VideoType.Iso || video.VideoType == VideoType.VideoFile ? Path.GetDirectoryName(video.Path) : video.Path;

            return Path.Combine(directory, "movie.xml");
        }
    }
}
