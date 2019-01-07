using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.LocalMetadata.Savers
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

        public PlaylistXmlSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger, IXmlReaderSettingsFactory xmlReaderSettingsFactory) : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger, xmlReaderSettingsFactory)
        {
        }
    }
}
