using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
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
    public class BoxSetXmlSaver : BaseXmlSaver
    {
        public override bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            return item is BoxSet && updateType >= ItemUpdateType.MetadataDownload;
        }

        protected override void WriteCustomElements(IHasMetadata item, XmlWriter writer)
        {
        }

        protected override string GetLocalSavePath(IHasMetadata item)
        {
            return Path.Combine(item.Path, "collection.xml");
        }

        public BoxSetXmlSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger, IXmlReaderSettingsFactory xmlReaderSettingsFactory) : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger, xmlReaderSettingsFactory)
        {
        }
    }
}
