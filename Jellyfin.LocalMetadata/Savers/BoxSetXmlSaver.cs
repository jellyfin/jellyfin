using System.IO;
using System.Xml;
using Jellyfin.Controller.Configuration;
using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Entities.Movies;
using Jellyfin.Controller.Library;
using Jellyfin.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LocalMetadata.Savers
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

        public BoxSetXmlSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger)
            : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger)
        {
        }
    }
}
