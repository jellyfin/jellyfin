using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;

namespace MediaBrowser.LocalMetadata.Savers
{
    public class SeasonXmlSaver : IMetadataFileSaver
    {
        public string Name
        {
            get
            {
                return "Media Browser Xml";
            }
        }

        private readonly IServerConfigurationManager _config;

        public SeasonXmlSaver(IServerConfigurationManager config)
        {
            _config = config;
        }
        
        /// <summary>
        /// Determines whether [is enabled for] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <returns><c>true</c> if [is enabled for] [the specified item]; otherwise, <c>false</c>.</returns>
        public bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
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

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        /// <summary>
        /// Saves the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public void Save(IHasMetadata item, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();

            builder.Append("<Item>");

            var season = (Season)item;

            if (season.IndexNumber.HasValue)
            {
                builder.Append("<SeasonNumber>" + SecurityElement.Escape(season.IndexNumber.Value.ToString(_usCulture)) + "</SeasonNumber>");
            }
            
            XmlSaverHelpers.AddCommonNodes((Season)item, builder);

            builder.Append("</Item>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new List<string>
            {
                "SeasonNumber"
            }, _config);
        }

        /// <summary>
        /// Gets the save path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        public string GetSavePath(IHasMetadata item)
        {
            return Path.Combine(item.Path, "season.xml");
        }
    }
}
