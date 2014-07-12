using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace MediaBrowser.XbmcMetadata.Savers
{
    public class MovieNfoSaver : BaseNfoSaver
    {
        public MovieNfoSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger) : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger)
        {
        }

        public override string GetSavePath(IHasMetadata item)
        {
            return GetMovieSavePath(item);
        }

        public static string GetMovieSavePath(IHasMetadata item)
        {
            var video = (Video)item;

            if (video.VideoType == VideoType.Dvd || video.VideoType == VideoType.BluRay || video.VideoType == VideoType.HdDvd)
            {
                var path = item.ContainingFolderPath;

                return Path.Combine(path, Path.GetFileNameWithoutExtension(path) + ".nfo");
            }

            return Path.ChangeExtension(item.Path, ".nfo");
        }

        protected override string GetRootElementName(IHasMetadata item)
        {
            return item is MusicVideo ? "musicvideo" : "movie";
        }

        public override bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            var video = item as Video;

            // Check parent for null to avoid running this against things like video backdrops
            if (video != null && !(item is Episode) && !video.IsOwnedItem)
            {
                return updateType >= ItemUpdateType.MetadataDownload;
            }

            return false;
        }

        protected override void WriteCustomElements(IHasMetadata item, XmlWriter writer)
        {
            var imdb = item.GetProviderId(MetadataProviders.Imdb);

            if (!string.IsNullOrEmpty(imdb))
            {
                writer.WriteElementString("id", imdb);
            }

            var musicVideo = item as MusicVideo;

            if (musicVideo != null)
            {
                if (!string.IsNullOrEmpty(musicVideo.Artist))
                {
                    writer.WriteElementString("artist", musicVideo.Artist);
                }
                if (!string.IsNullOrEmpty(musicVideo.Album))
                {
                    writer.WriteElementString("album", musicVideo.Album);
                }
            }

            var movie = item as Movie;

            if (movie != null)
            {
                if (!string.IsNullOrEmpty(movie.TmdbCollectionName))
                {
                    writer.WriteElementString("set", movie.TmdbCollectionName);
                }
            }
        }

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        protected override List<string> GetTagsUsed()
        {
            var list = new List<string>
            {
                    "album",
                    "artist",
                    "set",
                    "id"
            };

            return list;
        }
    }
}
