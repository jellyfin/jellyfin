using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;

namespace MediaBrowser.XbmcMetadata.Savers
{
    public class AlbumXmlSaver : IMetadataFileSaver
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataRepo;

        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _config;

        public AlbumXmlSaver(ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataRepo, IFileSystem fileSystem, IServerConfigurationManager config)
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
            return Path.Combine(item.Path, "album.nfo");
        }

        public void Save(IHasMetadata item, CancellationToken cancellationToken)
        {
            var album = (MusicAlbum)item;

            var builder = new StringBuilder();

            builder.Append("<album>");

            XmlSaverHelpers.AddCommonNodes(album, builder, _libraryManager, _userManager, _userDataRepo, _fileSystem, _config);

            var tracks = album.Tracks
                .ToList();

            var artists = tracks
                .SelectMany(i =>
                {
                    var list = new List<string>();

                    if (!string.IsNullOrEmpty(i.AlbumArtist))
                    {
                        list.Add(i.AlbumArtist);
                    }
                    list.AddRange(i.Artists);

                    return list;
                })
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var artist in artists)
            {
                builder.Append("<artist>" + SecurityElement.Escape(artist) + "</artist>");
            }

            AddTracks(tracks, builder);

            builder.Append("</album>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new List<string>
                {
                    "track",
                    "artist"
                });
        }

        public bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            return item is MusicAlbum && updateType >= ItemUpdateType.MetadataDownload;
        }

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        private void AddTracks(IEnumerable<Audio> tracks, StringBuilder builder)
        {
            foreach (var track in tracks.OrderBy(i => i.ParentIndexNumber ?? 0).ThenBy(i => i.IndexNumber ?? 0))
            {
                builder.Append("<track>");

                if (track.IndexNumber.HasValue)
                {
                    builder.Append("<position>" + SecurityElement.Escape(track.IndexNumber.Value.ToString(UsCulture)) + "</position>");
                }

                if (!string.IsNullOrEmpty(track.Name))
                {
                    builder.Append("<title>" + SecurityElement.Escape(track.Name) + "</title>");
                }

                if (track.RunTimeTicks.HasValue)
                {
                    var time = TimeSpan.FromTicks(track.RunTimeTicks.Value).ToString(@"mm\:ss");

                    builder.Append("<duration>" + SecurityElement.Escape(time) + "</duration>");
                }

                builder.Append("</track>");
            }
        }
    }
}
