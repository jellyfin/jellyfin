using Emby.Naming.Common;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.MediaInfo
{
    /// <summary>
    /// Resolves external subtitle files for <see cref="Video"/>.
    /// </summary>
    public class SubtitleResolver : MediaInfoResolver
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleResolver"/> class for external subtitle file processing.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizationManager">The localization manager.</param>
        /// <param name="mediaEncoder">The media encoder.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="namingOptions">The <see cref="NamingOptions"/> object containing FileExtensions, MediaDefaultFlags, MediaForcedFlags and MediaFlagDelimiters.</param>
        public SubtitleResolver(
            ILogger<SubtitleResolver> logger,
            ILocalizationManager localizationManager,
            IMediaEncoder mediaEncoder,
            IFileSystem fileSystem,
            NamingOptions namingOptions)
            : base(
                logger,
                localizationManager,
                mediaEncoder,
                fileSystem,
                namingOptions,
                DlnaProfileType.Subtitle)
        {
        }
    }
}
