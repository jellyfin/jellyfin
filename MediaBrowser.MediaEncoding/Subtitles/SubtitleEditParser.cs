using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Jellyfin.Extensions;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;
using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.SubtitleFormats;
using SubtitleFormat = Nikse.SubtitleEdit.Core.SubtitleFormats.SubtitleFormat;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    /// <summary>
    /// SubStation Alpha subtitle parser.
    /// </summary>
    public class SubtitleEditParser : ISubtitleParser
    {
        private readonly ILogger<SubtitleEditParser> _logger;
        private readonly Dictionary<string, SubtitleFormat[]> _subtitleFormats;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleEditParser"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public SubtitleEditParser(ILogger<SubtitleEditParser> logger)
        {
            _logger = logger;
            _subtitleFormats = GetSubtitleFormats()
                .Where(subtitleFormat => !string.IsNullOrEmpty(subtitleFormat.Extension))
                .GroupBy(subtitleFormat => subtitleFormat.Extension.TrimStart('.'), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public SubtitleTrackInfo Parse(Stream stream, string fileExtension)
        {
            var subtitle = new Subtitle();
            var lines = stream.ReadAllLines().ToList();

            if (!_subtitleFormats.TryGetValue(fileExtension, out var subtitleFormats))
            {
                throw new ArgumentException($"Unsupported file extension: {fileExtension}", nameof(fileExtension));
            }

            foreach (var subtitleFormat in subtitleFormats)
            {
                _logger.LogDebug(
                    "Trying to parse '{FileExtension}' subtitle using the {SubtitleFormatParser} format parser",
                    fileExtension,
                    subtitleFormat.Name);
                subtitleFormat.LoadSubtitle(subtitle, lines, fileExtension);
                if (subtitleFormat.ErrorCount == 0)
                {
                    break;
                }

                _logger.LogError(
                    "{ErrorCount} errors encountered while parsing '{FileExtension}' subtitle using the {SubtitleFormatParser} format parser",
                    subtitleFormat.ErrorCount,
                    fileExtension,
                    subtitleFormat.Name);
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
            => _subtitleFormats.ContainsKey(fileExtension);

        private IEnumerable<SubtitleFormat> GetSubtitleFormats()
        {
            var subtitleFormats = new List<SubtitleFormat>();
            var assembly = typeof(SubtitleFormat).Assembly;

            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsSubclassOf(typeof(SubtitleFormat)) || type.IsAbstract)
                {
                    continue;
                }

                try
                {
                    // It shouldn't be null, but the exception is caught if it is
                    var subtitleFormat = (SubtitleFormat)Activator.CreateInstance(type, true)!;
                    subtitleFormats.Add(subtitleFormat);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create instance of the subtitle format {SubtitleFormatType}", type.Name);
                }
            }

            return subtitleFormats;
        }
    }
}
