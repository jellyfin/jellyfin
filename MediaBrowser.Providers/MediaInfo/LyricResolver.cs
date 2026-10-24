using System;
using System.Collections.Generic;
using System.Linq;
using Emby.Naming.Common;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.MediaInfo;

/// <summary>
/// Resolves external lyric files for <see cref="Audio"/>.
/// </summary>
public class LyricResolver : MediaInfoResolver
{
    private readonly IReadOnlyCollection<string> _additionalFileExtensions;

    /// <summary>
    /// Initializes a new instance of the <see cref="LyricResolver"/> class for external subtitle file processing.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="localizationManager">The localization manager.</param>
    /// <param name="mediaEncoder">The media encoder.</param>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="namingOptions">The <see cref="NamingOptions"/> object containing FileExtensions, MediaDefaultFlags, MediaForcedFlags and MediaFlagDelimiters.</param>
    /// <param name="lyricParsers">The loaded lyric parsers.</param>
    public LyricResolver(
        ILogger<LyricResolver> logger,
        ILocalizationManager localizationManager,
        IMediaEncoder mediaEncoder,
        IFileSystem fileSystem,
        NamingOptions namingOptions,
        IEnumerable<ILyricParser> lyricParsers)
        : base(
            logger,
            localizationManager,
            mediaEncoder,
            fileSystem,
            namingOptions,
            DlnaProfileType.Lyric)
    {
        _additionalFileExtensions = lyricParsers
            .SelectMany(i => i.SupportedExtensions)
            .Where(i => !namingOptions.LyricFileExtensions.Contains(i, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <inheritdoc />
    protected override IReadOnlyCollection<string> AdditionalFileExtensions => _additionalFileExtensions;
}
