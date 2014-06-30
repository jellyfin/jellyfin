using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.XbmcMetadata.Savers
{
    public class SeriesXmlSaver : IMetadataFileSaver
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataRepo;

        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _config;

        public SeriesXmlSaver(IServerConfigurationManager config, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataRepo, IFileSystem fileSystem)
        {
            _config = config;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _userDataRepo = userDataRepo;
            _fileSystem = fileSystem;
        }

        public string Name
        {
            get
            {
                return "Xbmc Nfo";
            }
        }

        public string GetSavePath(IHasMetadata item)
        {
            return Path.Combine(item.Path, "tvshow.nfo");
        }

        public void Save(IHasMetadata item, CancellationToken cancellationToken)
        {
            var series = (Series)item;

            var builder = new StringBuilder();

            builder.Append("<tvshow>");

            XmlSaverHelpers.AddCommonNodes(series, builder, _libraryManager, _userManager, _userDataRepo, _fileSystem, _config);

            var tvdb = item.GetProviderId(MetadataProviders.Tvdb);

            if (!string.IsNullOrEmpty(tvdb))
            {
                builder.Append("<id>" + SecurityElement.Escape(tvdb) + "</id>");

                builder.AppendFormat("<episodeguide><url cache=\"{0}.xml\">http://www.thetvdb.com/api/1D62F2F90030C444/series/{0}/all/{1}.zip</url></episodeguide>", 
                    tvdb,
                    string.IsNullOrEmpty(_config.Configuration.PreferredMetadataLanguage) ? "en" : _config.Configuration.PreferredMetadataLanguage);
            }

            builder.Append("<season>-1</season>");
            builder.Append("<episode>-1</episode>");

            if (series.Status.HasValue)
            {
                builder.Append("<status>" + SecurityElement.Escape(series.Status.Value.ToString()) + "</status>");
            }

            if (!string.IsNullOrEmpty(series.AirTime))
            {
                builder.Append("<airs_time>" + SecurityElement.Escape(series.AirTime) + "</airs_time>");
            }

            if (series.AirDays.Count == 7)
            {
                builder.Append("<airs_dayofweek>" + SecurityElement.Escape("Daily") + "</airs_dayofweek>");
            }
            else if (series.AirDays.Count > 0)
            {
                builder.Append("<airs_dayofweek>" + SecurityElement.Escape(series.AirDays[0].ToString()) + "</airs_dayofweek>");
            }

            if (series.AnimeSeriesIndex.HasValue)
            {
                builder.Append("<animeseriesindex>" + SecurityElement.Escape(series.AnimeSeriesIndex.Value.ToString(CultureInfo.InvariantCulture)) + "</animeseriesindex>");
            }
            
            builder.Append("</tvshow>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new List<string>
                {
                    "id",
                    "episodeguide",
                    "season",
                    "episode",
                    "status",
                    "airs_time",
                    "airs_dayofweek",
                    "animeseriesindex"
                });
        }

        public bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            return item is Series && updateType >= ItemUpdateType.MetadataDownload;
        }
    }
}
