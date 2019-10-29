using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Savers
{
    public class SeriesNfoSaver : BaseNfoSaver
    {
        public SeriesNfoSaver(
            IFileSystem fileSystem,
            IServerConfigurationManager configurationManager,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IUserDataManager userDataManager,
            ILogger logger)
            : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger)
        {
        }

        /// <inheritdoc />
        protected override string GetLocalSavePath(BaseItem item)
            => Path.Combine(item.Path, "tvshow.nfo");

        /// <inheritdoc />
        protected override string GetRootElementName(BaseItem item)
            => "tvshow";

        /// <inheritdoc />
        public override bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
            => item.SupportsLocalMetadata && item is Series && updateType >= MinimumUpdateType;

        /// <inheritdoc />
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
                writer.WriteString(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "http://www.thetvdb.com/api/1D62F2F90030C444/series/{0}/all/{1}.zip",
                        tvdb,
                        language));
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

        /// <inheritdoc />
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
    }
}
