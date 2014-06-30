using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.XbmcMetadata.Configuration;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;

namespace MediaBrowser.XbmcMetadata.Savers
{
    public class ArtistXmlSaver : IMetadataFileSaver
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataRepo;

        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _config;

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        public ArtistXmlSaver(ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataRepo, IFileSystem fileSystem, IServerConfigurationManager config)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
            _userDataRepo = userDataRepo;
            _fileSystem = fileSystem;
            _config = config;
        }

        public string GetSavePath(IHasMetadata item)
        {
            return Path.Combine(item.Path, "artist.nfo");
        }

        public string Name
        {
            get
            {
                return "Xbmc Nfo";
            }
        }

        public void Save(IHasMetadata item, CancellationToken cancellationToken)
        {
            var artist = (MusicArtist)item;

            var builder = new StringBuilder();

            builder.Append("<artist>");

            XmlSaverHelpers.AddCommonNodes(artist, builder, _libraryManager, _userManager, _userDataRepo, _fileSystem, _config);

            if (artist.EndDate.HasValue)
            {
                var formatString = _config.GetNfoConfiguration().ReleaseDateFormat;

                builder.Append("<disbanded>" + SecurityElement.Escape(artist.EndDate.Value.ToString(formatString)) + "</disbanded>");
            }

            var albums = artist
                .RecursiveChildren
                .OfType<MusicAlbum>()
                .ToList();

            AddAlbums(albums, builder);

            builder.Append("</artist>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new List<string>
                {
                    "album",
                    "disbanded"
                });
        }

        public bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            return item is MusicArtist && updateType >= ItemUpdateType.MetadataDownload;
        }

        private void AddAlbums(IEnumerable<MusicAlbum> albums, StringBuilder builder)
        {
            foreach (var album in albums)
            {
                builder.Append("<album>");

                if (!string.IsNullOrEmpty(album.Name))
                {
                    builder.Append("<title>" + SecurityElement.Escape(album.Name) + "</title>");
                }

                if (album.ProductionYear.HasValue)
                {
                    builder.Append("<year>" + SecurityElement.Escape(album.ProductionYear.Value.ToString(UsCulture)) + "</year>");
                }
                
                builder.Append("</album>");
            }
        }
    }
}
