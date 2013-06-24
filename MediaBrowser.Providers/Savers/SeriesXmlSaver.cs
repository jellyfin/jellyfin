using System;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;
using MediaBrowser.Providers.TV;

namespace MediaBrowser.Providers.Savers
{
    public class SeriesXmlSaver : IMetadataSaver
    {
        private readonly IServerConfigurationManager _config;

        public SeriesXmlSaver(IServerConfigurationManager config)
        {
            _config = config;
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool Supports(BaseItem item)
        {
            if (!_config.Configuration.SaveLocalMeta || item.LocationType != LocationType.FileSystem)
            {
                return false;
            }

            return item is Series;
        }

        /// <summary>
        /// Saves the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public void Save(BaseItem item, CancellationToken cancellationToken)
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
                builder.Append("<Network>" + SecurityElement.Escape(item.Studios[0]) + "</Network>");
            }

            if (!string.IsNullOrEmpty(series.AirTime))
            {
                builder.Append("<Airs_Time>" + SecurityElement.Escape(series.AirTime) + "</Airs_Time>");
            }

            if (series.AirDays.Count == 7)
            {
                builder.Append("<Airs_DayOfWeek>" + SecurityElement.Escape("Daily") + "</Airs_DayOfWeek>");
            }
            else if (series.AirDays.Count > 0)
            {
                builder.Append("<Airs_DayOfWeek>" + SecurityElement.Escape(series.AirDays[0].ToString()) + "</Airs_DayOfWeek>");
            }

            XmlSaverHelpers.AddCommonNodes(item, builder);

            builder.Append("</Series>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new[]
                {
                    "id", 
                    "SeriesName",
                    "Status",
                    "Network",
                    "Airs_Time",
                    "Airs_DayOfWeek"
                });

            // Set last refreshed so that the provider doesn't trigger after the file save
            SeriesProviderFromXml.Current.SetLastRefreshed(item, DateTime.UtcNow);
        }

        /// <summary>
        /// Gets the save path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        public string GetSavePath(BaseItem item)
        {
            return Path.Combine(item.Path, "series.xml");
        }
    }
}
