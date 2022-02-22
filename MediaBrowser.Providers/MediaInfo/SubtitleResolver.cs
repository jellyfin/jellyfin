using Emby.Naming.Common;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Globalization;

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
        /// <param name="localizationManager">The localization manager.</param>
        /// <param name="mediaEncoder">The media encoder.</param>
        /// <param name="namingOptions">The <see cref="NamingOptions"/> object containing FileExtensions, MediaDefaultFlags, MediaForcedFlags and MediaFlagDelimiters.</param>
        public SubtitleResolver(
            ILocalizationManager localizationManager,
            IMediaEncoder mediaEncoder,
            NamingOptions namingOptions)
            : base(localizationManager, mediaEncoder, namingOptions, DlnaProfileType.Subtitle)
            {
        }
    }
}
