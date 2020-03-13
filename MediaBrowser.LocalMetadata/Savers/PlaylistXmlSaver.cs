using System.IO;
using System.Xml;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.LocalMetadata.Savers
{
    public class PlaylistXmlSaver : BaseXmlSaver
    {
        /// <summary>
        /// The default file name to use when creating a new playlist.
        /// </summary>
        public const string DefaultPlaylistFilename = "playlist.xml";

        public override bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            return item is Playlist && updateType >= ItemUpdateType.MetadataImport;
        }

        protected override void WriteCustomElements(BaseItem item, XmlWriter writer)
        {
            var game = (Playlist)item;

            if (!string.IsNullOrEmpty(game.PlaylistMediaType))
            {
                writer.WriteElementString("PlaylistMediaType", game.PlaylistMediaType);
            }
        }

        protected override string GetLocalSavePath(BaseItem item)
        {
            return GetSavePath(item.Path, FileSystem);
        }

        public static string GetSavePath(string itemPath, IFileSystem fileSystem)
        {
            var path = itemPath;

            if (Playlist.IsPlaylistFile(path))
            {
                return Path.ChangeExtension(itemPath, ".xml");
            }

            return Path.Combine(path, DefaultPlaylistFilename);
        }

        public PlaylistXmlSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger<PlaylistXmlSaver> logger)
            : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger)
        {
        }
    }
}
