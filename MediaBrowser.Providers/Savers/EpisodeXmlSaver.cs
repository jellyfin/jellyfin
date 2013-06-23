using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;
using MediaBrowser.Providers.TV;

namespace MediaBrowser.Providers.Savers
{
    public class EpisodeXmlSaver : IMetadataSaver
    {
        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool Supports(BaseItem item)
        {
            if (item.LocationType != LocationType.FileSystem)
            {
                return false;
            }

            return item is Episode;
        }

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        /// <summary>
        /// Saves the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public void Save(BaseItem item, CancellationToken cancellationToken)
        {
            var episode = (Episode)item;

            var builder = new StringBuilder();

            builder.Append("<Item>");

            if (!string.IsNullOrEmpty(item.Name))
            {
                builder.Append("<EpisodeName>" + SecurityElement.Escape(episode.Name) + "</EpisodeName>");
            }

            if (episode.IndexNumber.HasValue)
            {
                builder.Append("<EpisodeNumber>" + SecurityElement.Escape(episode.IndexNumber.Value.ToString(_usCulture)) + "</EpisodeNumber>");
            }

            if (episode.ParentIndexNumber.HasValue)
            {
                builder.Append("<SeasonNumber>" + SecurityElement.Escape(episode.ParentIndexNumber.Value.ToString(_usCulture)) + "</SeasonNumber>");
            }

            if (episode.PremiereDate.HasValue)
            {
                builder.Append("<FirstAired>" + SecurityElement.Escape(episode.PremiereDate.Value.ToString("yyyy-MM-dd")) + "</FirstAired>");
            }

            XmlSaverHelpers.AddCommonNodes(item, builder);
            XmlSaverHelpers.AppendMediaInfo(episode, builder);

            builder.Append("</Item>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath);

            // Set last refreshed so that the provider doesn't trigger after the file save
            EpisodeProviderFromXml.Current.SetLastRefreshed(item, DateTime.UtcNow);
        }

        /// <summary>
        /// Gets the save path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        public string GetSavePath(BaseItem item)
        {
            var filename = Path.ChangeExtension(Path.GetFileName(item.Path), ".xml");

            return Path.Combine(Path.GetDirectoryName(item.Path), "metadata", filename);
        }
    }
}
