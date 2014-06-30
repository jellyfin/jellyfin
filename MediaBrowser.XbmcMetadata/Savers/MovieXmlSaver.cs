using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;

namespace MediaBrowser.XbmcMetadata.Savers
{
    public class MovieXmlSaver : IMetadataFileSaver
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataRepo;

        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _config;
        
        public MovieXmlSaver(ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataRepo, IFileSystem fileSystem, IServerConfigurationManager config)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
            _userDataRepo = userDataRepo;
            _fileSystem = fileSystem;
            _config = config;
        }

        public string Name
        {
            get
            {
                return "Xbmc Nfo";
            }
        }

        public string GetSavePath(IHasMetadata item)
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

        public void Save(IHasMetadata item, CancellationToken cancellationToken)
        {
            var video = (Video)item;

            var builder = new StringBuilder();

            var tag = item is MusicVideo ? "musicvideo" : "movie";

            builder.Append("<" + tag + ">");

            XmlSaverHelpers.AddCommonNodes(video, builder, _libraryManager, _userManager, _userDataRepo, _fileSystem, _config);

            var imdb = item.GetProviderId(MetadataProviders.Imdb);

            if (!string.IsNullOrEmpty(imdb))
            {
                builder.Append("<id>" + SecurityElement.Escape(imdb) + "</id>");
            }

            var musicVideo = item as MusicVideo;

            if (musicVideo != null)
            {
                if (!string.IsNullOrEmpty(musicVideo.Artist))
                {
                    builder.Append("<artist>" + SecurityElement.Escape(musicVideo.Artist) + "</artist>");
                }
                if (!string.IsNullOrEmpty(musicVideo.Album))
                {
                    builder.Append("<album>" + SecurityElement.Escape(musicVideo.Album) + "</album>");
                }
            }

            var movie = item as Movie;

            if (movie != null)
            {
                if (!string.IsNullOrEmpty(movie.TmdbCollectionName))
                {
                    builder.Append("<set>" + SecurityElement.Escape(movie.TmdbCollectionName) + "</set>");
                }
            }

            XmlSaverHelpers.AddMediaInfo((Video)item, builder);

            builder.Append("</" + tag + ">");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new List<string>
                {
                    "album",
                    "artist",
                    "set",
                    "id"
                });
        }

        public bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
        {
            var locationType = item.LocationType;
            if (locationType == LocationType.Remote || locationType == LocationType.Virtual)
            {
                return false;
            }

            // If new metadata has been downloaded or metadata was manually edited, proceed
            if ((updateType & ItemUpdateType.MetadataDownload) == ItemUpdateType.MetadataDownload
                || (updateType & ItemUpdateType.MetadataEdit) == ItemUpdateType.MetadataEdit)
            {
                var video = item as Video;

                // Check parent for null to avoid running this against things like video backdrops
                if (video != null && !(item is Episode) && !video.IsOwnedItem)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
