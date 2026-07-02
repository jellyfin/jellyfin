using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Jellyfin.Extensions;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;
using Nikse.SubtitleEdit.Core.Common;
using SubtitleFormat = Nikse.SubtitleEdit.Core.SubtitleFormats.SubtitleFormat;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    /// <summary>
    /// Subtitle parser backed by the libse (Subtitle Edit) library.
    /// </summary>
    public class SubtitleEditParser : ISubtitleParser
    {
        private readonly ILogger<SubtitleEditParser> _logger;
        private readonly Dictionary<string, List<Type>> _subtitleFormatTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleEditParser"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public SubtitleEditParser(ILogger<SubtitleEditParser> logger)
        {
            _logger = logger;
            _subtitleFormatTypes = GetSubtitleFormatTypes();
        }

        /// <inheritdoc />
        public SubtitleTrackInfo Parse(Stream stream, string fileExtension)
        {
            var subtitle = new Subtitle();
            var lines = stream.ReadAllLines().ToList();

            if (!_subtitleFormatTypes.TryGetValue(fileExtension, out var subtitleFormatTypesForExtension))
            {
                throw new ArgumentException($"Unsupported file extension: {fileExtension}", nameof(fileExtension));
            }

            foreach (var subtitleFormatType in subtitleFormatTypesForExtension)
            {
                var subtitleFormat = (SubtitleFormat)Activator.CreateInstance(subtitleFormatType, true)!;
                _logger.LogDebug(
                    "Trying to parse '{FileExtension}' subtitle using the {SubtitleFormatParser} format parser",
                    fileExtension,
                    subtitleFormat.Name);
                subtitleFormat.LoadSubtitle(subtitle, lines, fileExtension);
                if (subtitleFormat.ErrorCount == 0)
                {
                    break;
                }
                else if (subtitleFormat.TryGetErrors(out var errors))
                {
                    _logger.LogError(
                        "{ErrorCount} errors encountered while parsing '{FileExtension}' subtitle using the {SubtitleFormatParser} format parser, errors: {Errors}",
                        subtitleFormat.ErrorCount,
                        fileExtension,
                        subtitleFormat.Name,
                        errors);
                }
                else
                {
                    _logger.LogError(
                        "{ErrorCount} errors encountered while parsing '{FileExtension}' subtitle using the {SubtitleFormatParser} format parser",
                        subtitleFormat.ErrorCount,
                        fileExtension,
                        subtitleFormat.Name);
                }
            }

            if (subtitle.Paragraphs.Count == 0)
            {
                throw new ArgumentException("Unsupported format: " + fileExtension);
            }

            var trackInfo = new SubtitleTrackInfo();
            int len = subtitle.Paragraphs.Count;
            var trackEvents = new SubtitleTrackEvent[len];
            for (int i = 0; i < len; i++)
            {
                var p = subtitle.Paragraphs[i];
                trackEvents[i] = new SubtitleTrackEvent(p.Number.ToString(CultureInfo.InvariantCulture), p.Text)
                {
                    StartPositionTicks = p.StartTime.TimeSpan.Ticks,
                    EndPositionTicks = p.EndTime.TimeSpan.Ticks
                };
            }

            trackInfo.TrackEvents = trackEvents;
            return trackInfo;
        }

        /// <inheritdoc />
        public bool SupportsFileExtension(string fileExtension)
            => _subtitleFormatTypes.ContainsKey(fileExtension);

        private Dictionary<string, List<Type>> GetSubtitleFormatTypes()
        {
            var subtitleFormatTypes = new Dictionary<string, List<Type>>(StringComparer.OrdinalIgnoreCase);
            var assembly = typeof(SubtitleFormat).Assembly;

            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsSubclassOf(typeof(SubtitleFormat)) || type.IsAbstract)
                {
                    continue;
                }

                try
                {
                    var tempInstance = (SubtitleFormat)Activator.CreateInstance(type, true)!;

                    // Collect the primary extension and any alternate extensions defined by the format.
                    // This is necessary because some formats report a different string via ffprobe than
                    // their primary Extension — e.g. WebVTT has Extension=".vtt" but ffprobe reports
                    // the codec as "webvtt", which libse exposes via AlternateExtensions.
                    // Without indexing AlternateExtensions, a lookup for "webvtt" would fail and fall
                    // through to a lossy libse VTT→VTT roundtrip that strips cue settings
                    // (position, line, align, size), breaking subtitle positioning on clients like ExoPlayer.
                    var extensions = new List<string>();

                    var primaryExtension = tempInstance.Extension.TrimStart('.');
                    if (!string.IsNullOrEmpty(primaryExtension))
                    {
                        extensions.Add(primaryExtension);
                    }

                    // AlternateExtensions returns an empty list by default; formats that override it
                    // (e.g. WebVTT returning "webvtt") are registered here alongside the primary extension.
                    foreach (var altExtension in tempInstance.AlternateExtensions)
                    {
                        var trimmed = altExtension.TrimStart('.');
                        if (!string.IsNullOrEmpty(trimmed))
                        {
                            extensions.Add(trimmed);
                        }
                    }

                    foreach (var ext in extensions)
                    {
                        // Store only the type, we will instantiate from it later
                        if (!subtitleFormatTypes.TryGetValue(ext, out var subtitleFormatTypesForExtension))
                        {
                            subtitleFormatTypes[ext] = [type];
                        }
                        else
                        {
                            subtitleFormatTypesForExtension.Add(type);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create instance of the subtitle format {SubtitleFormatType}", type.Name);
                }
            }

            return subtitleFormatTypes;
        }
    }
}
