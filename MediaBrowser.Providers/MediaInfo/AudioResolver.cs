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
    /// Resolves external audio files for <see cref="Video"/>.
    /// </summary>
    public class AudioResolver : MediaInfoResolver
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioResolver"/> class for external audio file processing.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizationManager">The localization manager.</param>
        /// <param name="mediaEncoder">The media encoder.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="namingOptions">The <see cref="NamingOptions"/> object containing FileExtensions, MediaDefaultFlags, MediaForcedFlags and MediaFlagDelimiters.</param>
        public AudioResolver(
            ILogger<AudioResolver> logger,
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
                DlnaProfileType.Audio)
        {
        }
    }
}
