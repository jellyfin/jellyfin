using System.IO;
using System.Xml;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.LocalMetadata.Savers
{
    public class BoxSetXmlSaver : BaseXmlSaver
    {
        public override bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            return item is BoxSet && updateType >= ItemUpdateType.MetadataDownload;
        }

        protected override void WriteCustomElements(BaseItem item, XmlWriter writer)
        {
        }

        protected override string GetLocalSavePath(BaseItem item)
        {
            return Path.Combine(item.Path, "collection.xml");
        }

        public BoxSetXmlSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger<BoxSetXmlSaver> logger)
            : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger)
        {
        }
    }
}
