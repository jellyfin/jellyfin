using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace MediaBrowser.XbmcMetadata.Savers
{
    public class SeasonNfoSaver : BaseNfoSaver
    {
        public SeasonNfoSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager) : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager)
        {
        }

        public override string GetSavePath(IHasMetadata item)
        {
            return Path.Combine(item.Path, "season.nfo");
        }

        protected override string GetRootElementName(IHasMetadata item)
        {
            return "season";
        }

        public override bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            if (!(item is Season))
            {
                return false;
            }

            return updateType >= ItemUpdateType.MetadataDownload || (updateType >= ItemUpdateType.MetadataImport && File.Exists(GetSavePath(item)));
        }

        protected override void WriteCustomElements(IHasMetadata item, XmlWriter writer)
        {
            var season = (Season)item;

            if (season.IndexNumber.HasValue)
            {
                writer.WriteElementString("seasonnumber", season.IndexNumber.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        protected override List<string> GetTagsUsed()
        {
            var list = base.GetTagsUsed();

            list.Add("seasonnumber");

            return list;
        }
    }
}
