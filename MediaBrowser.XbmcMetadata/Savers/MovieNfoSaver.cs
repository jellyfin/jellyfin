using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Savers
{
    public class MovieNfoSaver : BaseNfoSaver
    {
        public MovieNfoSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger)
            : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger)
        {
        }

        /// <inheritdoc />
        protected override string GetLocalSavePath(BaseItem item)
            => GetMovieSavePaths(new ItemInfo(item)).FirstOrDefault();

        public static IEnumerable<string> GetMovieSavePaths(ItemInfo item)
        {
            if (item.VideoType == VideoType.Dvd && !item.IsPlaceHolder)
            {
                var path = item.ContainingFolderPath;

                yield return Path.Combine(path, "VIDEO_TS", "VIDEO_TS.nfo");
            }

            if (!item.IsPlaceHolder && (item.VideoType == VideoType.Dvd || item.VideoType == VideoType.BluRay))
            {
                var path = item.ContainingFolderPath;

                yield return Path.Combine(path, Path.GetFileName(path) + ".nfo");
            }
            else
            {
                // http://kodi.wiki/view/NFO_files/Movies
                // movie.nfo will override all and any .nfo files in the same folder as the media files if you use the "Use foldernames for lookups" setting. If you don't, then moviename.nfo is used
                //if (!item.IsInMixedFolder && item.ItemType == typeof(Movie))
                //{
                //    list.Add(Path.Combine(item.ContainingFolderPath, "movie.nfo"));
                //}

                yield return Path.ChangeExtension(item.Path, ".nfo");

                if (!item.IsInMixedFolder)
                {
                    yield return Path.Combine(item.ContainingFolderPath, "movie.nfo");
                }
            }
        }

        /// <inheritdoc />
        protected override string GetRootElementName(BaseItem item)
            => item is MusicVideo ? "musicvideo" : "movie";

        /// <inheritdoc />
        public override bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            // Check parent for null to avoid running this against things like video backdrops
            if (item is Video video && !(item is Episode) && !video.ExtraType.HasValue)
            {
                return updateType >= MinimumUpdateType;
            }

            return false;
        }

        /// <inheritdoc />
        protected override void WriteCustomElements(BaseItem item, XmlWriter writer)
        {
            var imdb = item.GetProviderId(MetadataProviders.Imdb);

            if (!string.IsNullOrEmpty(imdb))
            {
                writer.WriteElementString("id", imdb);
            }

            if (item is MusicVideo musicVideo)
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

            if (item is Movie movie)
            {
                if (!string.IsNullOrEmpty(movie.CollectionName))
                {
                    writer.WriteElementString("set", movie.CollectionName);
                }
            }
        }

        /// <inheritdoc />
        protected override List<string> GetTagsUsed(BaseItem item)
        {
            var list = base.GetTagsUsed(item);
            list.AddRange(new string[]
            {
                "album",
                "artist",
                "set",
                "id"
            });

            return list;
        }
    }
}
