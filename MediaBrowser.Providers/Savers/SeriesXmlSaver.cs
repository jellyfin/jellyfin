using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;

namespace MediaBrowser.Providers.Savers
{
    public class SeriesXmlSaver : IMetadataFileSaver
    {
        private readonly IServerConfigurationManager _config;

        public SeriesXmlSaver(IServerConfigurationManager config)
        {
            _config = config;
        }

        public string Name
        {
            get
            {
                return "Media Browser Xml";
            }
        }

        /// <summary>
        /// Determines whether [is enabled for] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <returns><c>true</c> if [is enabled for] [the specified item]; otherwise, <c>false</c>.</returns>
        public bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
        {
            var locationType = item.LocationType;
            if (locationType == LocationType.Remote || locationType == LocationType.Virtual)
            {
                return false;
            }

            var wasMetadataEdited = (updateType & ItemUpdateType.MetadataEdit) == ItemUpdateType.MetadataEdit;
            var wasMetadataDownloaded = (updateType & ItemUpdateType.MetadataDownload) == ItemUpdateType.MetadataDownload;

            // If new metadata has been downloaded and save local is on
            if (item.IsSaveLocalMetadataEnabled() && (wasMetadataEdited || wasMetadataDownloaded))
            {
                return item is Series;
            }

            return false;
        }

        /// <summary>
        /// Saves the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public void Save(IHasMetadata item, CancellationToken cancellationToken)
        {
            var series = (Series)item;

            var builder = new StringBuilder();

            builder.Append("<Series>");

            var tvdb = item.GetProviderId(MetadataProviders.Tvdb);

            if (!string.IsNullOrEmpty(tvdb))
            {
                builder.Append("<id>" + SecurityElement.Escape(tvdb) + "</id>");
            }

            if (!string.IsNullOrEmpty(item.Name))
            {
                builder.Append("<SeriesName>" + SecurityElement.Escape(item.Name) + "</SeriesName>");
            }

            if (series.Status.HasValue)
            {
                builder.Append("<Status>" + SecurityElement.Escape(series.Status.Value.ToString()) + "</Status>");
            }

            if (series.Studios.Count > 0)
            {
                builder.Append("<Network>" + SecurityElement.Escape(series.Studios[0]) + "</Network>");
            }

            if (!string.IsNullOrEmpty(series.AirTime))
            {
                builder.Append("<Airs_Time>" + SecurityElement.Escape(series.AirTime) + "</Airs_Time>");
            }

            if (series.AirDays != null)
            {
                if (series.AirDays.Count == 7)
                {
                    builder.Append("<Airs_DayOfWeek>" + SecurityElement.Escape("Daily") + "</Airs_DayOfWeek>");
                }
                else if (series.AirDays.Count > 0)
                {
                    builder.Append("<Airs_DayOfWeek>" + SecurityElement.Escape(series.AirDays[0].ToString()) + "</Airs_DayOfWeek>");
                }
            }

            if (series.PremiereDate.HasValue)
            {
                builder.Append("<FirstAired>" + SecurityElement.Escape(series.PremiereDate.Value.ToLocalTime().ToString("yyyy-MM-dd")) + "</FirstAired>");
            }

            XmlSaverHelpers.AddCommonNodes(series, builder);

            builder.Append("</Series>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new List<string>
                {
                    "id", 
                    "SeriesName",
                    "Status",
                    "Network",
                    "Airs_Time",
                    "Airs_DayOfWeek",
                    "FirstAired",

                    // Don't preserve old series node
                    "Series"
                });
        }

        /// <summary>
        /// Gets the save path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        public string GetSavePath(IHasMetadata item)
        {
            return Path.Combine(item.Path, "series.xml");
        }
    }
}
