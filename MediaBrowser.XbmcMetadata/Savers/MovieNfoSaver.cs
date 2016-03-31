using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using CommonIO;

namespace MediaBrowser.XbmcMetadata.Savers
{
    public class MovieNfoSaver : BaseNfoSaver
    {
        public MovieNfoSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger) : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger)
        {
        }

        protected override string GetLocalSavePath(IHasMetadata item)
        {
            return GetMovieSavePaths(new ItemInfo(item), FileSystem).FirstOrDefault();
        }

        public static List<string> GetMovieSavePaths(ItemInfo item, IFileSystem fileSystem)
        {
            var list = new List<string>();

            if (item.VideoType == VideoType.Dvd && !item.IsPlaceHolder)
            {
                var path = item.ContainingFolderPath;

                list.Add(Path.Combine(path, "VIDEO_TS", "VIDEO_TS.nfo"));
            }

            if (item.VideoType == VideoType.Dvd || item.VideoType == VideoType.BluRay || item.VideoType == VideoType.HdDvd)
            {
                var path = item.ContainingFolderPath;

                list.Add(Path.Combine(path, Path.GetFileName(path) + ".nfo"));
            }
            else
            {
                list.Add(Path.ChangeExtension(item.Path, ".nfo"));
            }

            return list;
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
                return updateType >= MinimumUpdateType;
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
                foreach (var artist in musicVideo.Artists)
                {
                    writer.WriteElementString("artist", artist);
                }
                if (!string.IsNullOrEmpty(musicVideo.Album))
                {
                    writer.WriteElementString("album", musicVideo.Album);
                }
            }

            var movie = item as Movie;

            if (movie != null)
            {
                if (!string.IsNullOrEmpty(movie.CollectionName))
                {
                    writer.WriteElementString("set", movie.CollectionName);
                }
            }
        }

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
