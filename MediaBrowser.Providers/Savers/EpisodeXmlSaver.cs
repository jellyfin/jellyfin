using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Providers.TV;
using System;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;

namespace MediaBrowser.Providers.Savers
{
    public class EpisodeXmlSaver : IMetadataSaver
    {
        private readonly IServerConfigurationManager _config;
        private readonly IItemRepository _itemRepository;

        /// <summary>
        /// Determines whether [is enabled for] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <returns><c>true</c> if [is enabled for] [the specified item]; otherwise, <c>false</c>.</returns>
        public bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            var wasMetadataEdited = (updateType & ItemUpdateType.MetadataEdit) == ItemUpdateType.MetadataEdit;
            var wasMetadataDownloaded = (updateType & ItemUpdateType.MetadataDownload) == ItemUpdateType.MetadataDownload;

            // If new metadata has been downloaded and save local is on, OR metadata was manually edited, proceed
            if ((_config.Configuration.SaveLocalMeta && (wasMetadataEdited || wasMetadataDownloaded)) || wasMetadataEdited)
            {
                return item is Episode;
            }

            return false;
        }

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public EpisodeXmlSaver(IServerConfigurationManager config, IItemRepository itemRepository)
        {
            _config = config;
            _itemRepository = itemRepository;
        }

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
            XmlSaverHelpers.AddMediaInfo(episode, builder, _itemRepository);

            builder.Append("</Item>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new[]
                {
                    "FirstAired",
                    "SeasonNumber",
                    "EpisodeNumber",
                    "EpisodeName"
                });

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
