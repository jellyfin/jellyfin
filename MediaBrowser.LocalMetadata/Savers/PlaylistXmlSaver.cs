using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.LocalMetadata.Savers
{
    public class PlaylistXmlSaver : BaseXmlSaver
    {
        public override bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            return item is Playlist && updateType >= ItemUpdateType.MetadataImport;
        }

        protected override List<string> GetTagsUsed()
        {
            var list = new List<string>
            {
                "OwnerUserId",
                "PlaylistMediaType"
            };

            return list;
        }

        protected override void WriteCustomElements(IHasMetadata item, XmlWriter writer)
        {
            var game = (Playlist)item;

            if (!string.IsNullOrEmpty(game.PlaylistMediaType))
            {
                writer.WriteElementString("PlaylistMediaType", game.PlaylistMediaType);
            }
        }

        protected override string GetLocalSavePath(IHasMetadata item)
        {
            return Path.Combine(item.Path, "playlist.xml");
        }

        public PlaylistXmlSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger, IXmlReaderSettingsFactory xmlReaderSettingsFactory) : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger, xmlReaderSettingsFactory)
        {
        }
    }
}
