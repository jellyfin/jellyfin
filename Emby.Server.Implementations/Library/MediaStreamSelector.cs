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

                if (defaultStream != null)
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
                .OrderBy(i =>
                {
                    var index = languagePreferences.FindIndex(x => string.Equals(x, i.Language, StringComparison.OrdinalIgnoreCase));

                    return index == -1 ? 100 : index;
                })
                .ThenBy(i => GetBooleanOrderBy(i.IsDefault))
                .ThenBy(i => GetBooleanOrderBy(i.SupportsExternalStream))
                .ThenBy(i => GetBooleanOrderBy(i.IsTextSubtitleStream))
                .ThenBy(i => GetBooleanOrderBy(i.IsExternal))
                .ThenBy(i => i.Index);
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

            var sortedStreams = GetSortedStreams(streams, MediaStreamType.Subtitle, preferredLanguages);

            var filteredStreams = new List<MediaStream>();

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
                // always load the most suitable full subtitles
                filteredStreams = sortedStreams.Where(s => !s.IsForced).ToList();
            }
            else if (mode == SubtitlePlaybackMode.OnlyForced)
            {
                // always load the most suitable full subtitles
                filteredStreams = sortedStreams.Where(s => s.IsForced).ToList();
            }

            // load forced subs if we have found no suitable full subtitles
            var iterStreams = filteredStreams.Count == 0
                ? sortedStreams.Where(s => s.IsForced && string.Equals(s.Language, audioTrackLanguage, StringComparison.OrdinalIgnoreCase))
                : filteredStreams;

            foreach (var stream in iterStreams)
            {
                stream.Score = GetSubtitleScore(stream, preferredLanguages);
            }
        }

        private static int GetSubtitleScore(MediaStream stream, IReadOnlyList<string> languagePreferences)
        {
            var values = new List<int>();

            var index = languagePreferences.FindIndex(x => string.Equals(x, stream.Language, StringComparison.OrdinalIgnoreCase));

            values.Add(index == -1 ? 0 : 100 - index);

            values.Add(stream.IsForced ? 1 : 0);
            values.Add(stream.IsDefault ? 1 : 0);
            values.Add(stream.SupportsExternalStream ? 1 : 0);
            values.Add(stream.IsTextSubtitleStream ? 1 : 0);
            values.Add(stream.IsExternal ? 1 : 0);

            values.Reverse();
            var scale = 1;
            var score = 0;

            foreach (var value in values)
            {
                score += scale * (value + 1);
                scale *= 10;
            }

            return score;
        }

        private static int GetBooleanOrderBy(bool value)
        {
            return value ? 0 : 1;
        }
    }
}
