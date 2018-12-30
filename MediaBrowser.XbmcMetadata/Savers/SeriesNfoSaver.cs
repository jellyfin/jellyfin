using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.XbmcMetadata.Savers
{
    public class SeriesNfoSaver : BaseNfoSaver
    {
        protected override string GetLocalSavePath(BaseItem item)
        {
            return Path.Combine(item.Path, "tvshow.nfo");
        }

        protected override string GetRootElementName(BaseItem item)
        {
            return "tvshow";
        }

        public override bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            return item is Series && updateType >= MinimumUpdateType;
        }

        protected override void WriteCustomElements(BaseItem item, XmlWriter writer)
        {
            var series = (Series)item;

            var tvdb = item.GetProviderId(MetadataProviders.Tvdb);

            if (!string.IsNullOrEmpty(tvdb))
            {
                writer.WriteElementString("id", tvdb);

                writer.WriteStartElement("episodeguide");

                var language = item.GetPreferredMetadataLanguage();
                language = string.IsNullOrEmpty(language)
                    ? "en"
                    : language;

                writer.WriteStartElement("url");
                writer.WriteAttributeString("cache", string.Format("{0}.xml", tvdb));
                writer.WriteString(string.Format("http://www.thetvdb.com/api/1D62F2F90030C444/series/{0}/all/{1}.zip", tvdb, language));
                writer.WriteEndElement();
                
                writer.WriteEndElement();
            }

            writer.WriteElementString("season", "-1");
            writer.WriteElementString("episode", "-1");

            if (series.Status.HasValue)
            {
                writer.WriteElementString("status", series.Status.Value.ToString());
            }
        }

        protected override List<string> GetTagsUsed(BaseItem item)
        {
            var list = base.GetTagsUsed(item);
            list.AddRange(new string[]
            {
                "id",
                "episodeguide",
                "season",
                "episode",
                "status",
                "displayorder"
            });
            return list;
        }

        public SeriesNfoSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger, IXmlReaderSettingsFactory xmlReaderSettingsFactory) : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger, xmlReaderSettingsFactory)
        {
        }
    }
}
