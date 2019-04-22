using System.IO;
using System.Xml;
using Jellyfin.Controller.Configuration;
using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Library;
using Jellyfin.Controller.Playlists;
using Jellyfin.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LocalMetadata.Savers
{
    public class PlaylistXmlSaver : BaseXmlSaver
    {
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

            return Path.Combine(path, "playlist.xml");
        }

        public PlaylistXmlSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger)
            : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger)
        {
        }
    }
}
