using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.LocalMetadata.Savers
{
    public class GameSystemXmlSaver : BaseXmlSaver
    {
        public GameSystemXmlSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger, IXmlReaderSettingsFactory xmlReaderSettingsFactory) : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger, xmlReaderSettingsFactory)
        {
        }

        public override bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            return item is GameSystem && updateType >= ItemUpdateType.MetadataDownload;
        }

        protected override void WriteCustomElements(BaseItem item, XmlWriter writer)
        {
            var gameSystem = (GameSystem)item;

            if (!string.IsNullOrEmpty(gameSystem.GameSystemName))
            {
                writer.WriteElementString("GameSystem", gameSystem.GameSystemName);
            }
        }

        protected override string GetLocalSavePath(BaseItem item)
        {
            return Path.Combine(item.Path, "gamesystem.xml");
        }

        protected override string GetRootElementName(BaseItem item)
        {
            return "Item";
        }
    }
}
