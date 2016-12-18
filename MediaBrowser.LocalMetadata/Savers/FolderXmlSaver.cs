using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.LocalMetadata.Savers
{
    public class FolderXmlSaver : BaseXmlSaver
    {
        protected override string GetLocalSavePath(IHasMetadata item)
        {
            return Path.Combine(item.Path, "folder.xml");
        }

        protected override string GetRootElementName(IHasMetadata item)
        {
            return "Item";
        }

        public override bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            if (item is Folder)
            {
                if (!(item is Series) && !(item is BoxSet) && !(item is MusicArtist) && !(item is MusicAlbum) &&
                    !(item is Season) &&
                    !(item is GameSystem) &&
                    !(item is Playlist))
                {
                    return updateType >= ItemUpdateType.MetadataEdit;
                }
            }

            return false;
        }

        protected override void WriteCustomElements(IHasMetadata item, XmlWriter writer)
        {
        }

        public FolderXmlSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger, IXmlReaderSettingsFactory xmlReaderSettingsFactory) : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger, xmlReaderSettingsFactory)
        {
        }
    }
}
