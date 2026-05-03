using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;
using MediaBrowser.XbmcMetadata.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediaBrowser.XbmcMetadata.Savers
{
    /// <summary>
    /// Nfo saver for seasons.
    /// </summary>
    public class SeasonNfoSaver : BaseNfoSaver
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SeasonNfoSaver"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="xbmcMetadataOptions">The NFO metadata options.</param>
        /// <param name="serverConfig">The server configuration.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="userDataManager">The user data manager.</param>
        /// <param name="logger">The logger.</param>
        public SeasonNfoSaver(
            IFileSystem fileSystem,
            IOptions<XbmcMetadataOptions> xbmcMetadataOptions,
            IOptions<ServerConfiguration> serverConfig,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IUserDataManager userDataManager,
            ILogger<SeasonNfoSaver> logger)
            : base(fileSystem, xbmcMetadataOptions, serverConfig, libraryManager, userManager, userDataManager, logger)
        {
        }

        /// <inheritdoc />
        protected override string GetLocalSavePath(BaseItem item)
            => Path.Combine(item.Path, "season.nfo");

        /// <inheritdoc />
        protected override string GetRootElementName(BaseItem item)
            => "season";

        /// <inheritdoc />
        public override bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            if (item is not Season)
            {
                return false;
            }

            return updateType >= MinimumUpdateType || (updateType >= ItemUpdateType.MetadataImport && File.Exists(GetSavePath(item)));
        }

        /// <inheritdoc />
        protected override void WriteCustomElements(BaseItem item, XmlWriter writer)
        {
            var season = (Season)item;

            if (season.IndexNumber.HasValue)
            {
                writer.WriteElementString("seasonnumber", season.IndexNumber.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <inheritdoc />
        protected override IEnumerable<string> GetTagsUsed(BaseItem item)
        {
            foreach (var tag in base.GetTagsUsed(item))
            {
                yield return tag;
            }

            yield return "seasonnumber";
        }
    }
}
