#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Model.Entities;

namespace Emby.Server.Implementations.Library
{
    public static class MediaStreamSelector
    {
        public static int? GetDefaultAudioStreamIndex(IReadOnlyList<MediaStream> streams, IReadOnlyList<string> preferredLanguages, bool preferDefaultTrack)
        {
            var sortedStreams = GetSortedStreams(streams, MediaStreamType.Audio, preferredLanguages).ToList();

            if (preferDefaultTrack)
            {
                var defaultStream = sortedStreams.FirstOrDefault(i => i.IsDefault);

                if (defaultStream is not null)
                {
                    return defaultStream.Index;
                }
            }

            return sortedStreams.FirstOrDefault()?.Index;
        }

        public static int? GetDefaultSubtitleStreamIndex(
            IEnumerable<MediaStream> streams,
            IReadOnlyList<string> preferredLanguages,
            SubtitlePlaybackMode mode,
            string audioTrackLanguage)
        {
            if (mode == SubtitlePlaybackMode.None)
            {
                return null;
            }

            var sortedStreams = streams
                .Where(i => i.Type == MediaStreamType.Subtitle)
                .OrderByDescending(x => x.IsExternal)
                .ThenByDescending(x => x.IsForced && string.Equals(x.Language, audioTrackLanguage, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(x => x.IsForced)
                .ThenByDescending(x => x.IsDefault)
                .ThenByDescending(x => preferredLanguages.Contains(x.Language, StringComparison.OrdinalIgnoreCase))
                .ToList();

            MediaStream? stream = null;
            if (mode == SubtitlePlaybackMode.Default)
            {
                // Load subtitles according to external, forced and default flags.
                stream = sortedStreams.FirstOrDefault(x => x.IsExternal || x.IsForced || x.IsDefault);
            }
            else if (mode == SubtitlePlaybackMode.Smart)
            {
                // Only attempt to load subtitles if the audio language is not one of the user's preferred subtitle languages.
                // If no subtitles of preferred language available, use default behaviour.
                if (!preferredLanguages.Contains(audioTrackLanguage, StringComparison.OrdinalIgnoreCase))
                {
                    stream = sortedStreams.FirstOrDefault(x => preferredLanguages.Contains(x.Language, StringComparison.OrdinalIgnoreCase)) ??
                        sortedStreams.FirstOrDefault(x => x.IsExternal || x.IsForced || x.IsDefault);
                }
                else
                {
                    // Respect forced flag.
                    stream = sortedStreams.FirstOrDefault(x => x.IsForced);
                }
            }
            else if (mode == SubtitlePlaybackMode.Always)
            {
                // Always load (full/non-forced) subtitles of the user's preferred subtitle language if possible, otherwise default behaviour.
                stream = sortedStreams.FirstOrDefault(x => !x.IsForced && preferredLanguages.Contains(x.Language, StringComparison.OrdinalIgnoreCase)) ??
                    sortedStreams.FirstOrDefault(x => x.IsExternal || x.IsForced || x.IsDefault);
            }
            else if (mode == SubtitlePlaybackMode.OnlyForced)
            {
                // Only load subtitles that are flagged forced.
                stream = sortedStreams.FirstOrDefault(x => x.IsForced);
            }

            return stream?.Index;
        }

        private static IEnumerable<MediaStream> GetSortedStreams(IEnumerable<MediaStream> streams, MediaStreamType type, IReadOnlyList<string> languagePreferences)
        {
            // Give some preference to external text subs for better performance
            return streams
                .Where(i => i.Type == type)
                .OrderByDescending(i => GetStreamScore(i, languagePreferences));
        }

        public static void SetSubtitleStreamScores(
            IReadOnlyList<MediaStream> streams,
            IReadOnlyList<string> preferredLanguages,
            SubtitlePlaybackMode mode,
            string audioTrackLanguage)
        {
            if (mode == SubtitlePlaybackMode.None)
            {
                return;
            }

            var sortedStreams = GetSortedStreams(streams, MediaStreamType.Subtitle, preferredLanguages).ToList();

            List<MediaStream>? filteredStreams = null;

            if (mode == SubtitlePlaybackMode.Default)
            {
                // Prefer embedded metadata over smart logic
                filteredStreams = sortedStreams.Where(s => s.IsForced || s.IsDefault)
                    .ToList();
            }
            else if (mode == SubtitlePlaybackMode.Smart)
            {
                // Prefer smart logic over embedded metadata
                if (!preferredLanguages.Contains(audioTrackLanguage, StringComparison.OrdinalIgnoreCase))
                {
                    filteredStreams = sortedStreams.Where(s => !s.IsForced && preferredLanguages.Contains(s.Language, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
            }
            else if (mode == SubtitlePlaybackMode.Always)
            {
                // Always load the most suitable full subtitles
                filteredStreams = sortedStreams.Where(s => !s.IsForced).ToList();
            }
            else if (mode == SubtitlePlaybackMode.OnlyForced)
            {
                // Always load the most suitable full subtitles
                filteredStreams = sortedStreams.Where(s => s.IsForced).ToList();
            }

            // Load forced subs if we have found no suitable full subtitles
            var iterStreams = filteredStreams is null || filteredStreams.Count == 0
                ? sortedStreams.Where(s => s.IsForced && string.Equals(s.Language, audioTrackLanguage, StringComparison.OrdinalIgnoreCase))
                : filteredStreams;

            foreach (var stream in iterStreams)
            {
                stream.Score = GetStreamScore(stream, preferredLanguages);
            }
        }

        internal static int GetStreamScore(MediaStream stream, IReadOnlyList<string> languagePreferences)
        {
            var index = languagePreferences.FindIndex(x => string.Equals(x, stream.Language, StringComparison.OrdinalIgnoreCase));
            var score = index == -1 ? 1 : 101 - index;
            score = (score * 10) + (stream.IsForced ? 2 : 1);
            score = (score * 10) + (stream.IsDefault ? 2 : 1);
            score = (score * 10) + (stream.SupportsExternalStream ? 2 : 1);
            score = (score * 10) + (stream.IsTextSubtitleStream ? 2 : 1);
            score = (score * 10) + (stream.IsExternal ? 2 : 1);
            return score;
        }
    }
}
