#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;

namespace Emby.Server.Implementations.Library
{
    public static class MediaStreamSelector
    {
        public static int? GetDefaultAudioStreamIndex(List<MediaStream> streams, string[] preferredLanguages, bool preferDefaultTrack)
        {
            streams = GetSortedStreams(streams, MediaStreamType.Audio, preferredLanguages)
               .ToList();

            if (preferDefaultTrack)
            {
                var defaultStream = streams.FirstOrDefault(i => i.IsDefault);

                if (defaultStream != null)
                {
                    return defaultStream.Index;
                }
            }

            var stream = streams.FirstOrDefault();

            if (stream != null)
            {
                return stream.Index;
            }

            return null;
        }

        public static int? GetDefaultSubtitleStreamIndex(
            List<MediaStream> streams,
            string[] preferredLanguages,
            SubtitlePlaybackMode mode,
            string audioTrackLanguage)
        {
            streams = GetSortedStreams(streams, MediaStreamType.Subtitle, preferredLanguages)
                .ToList();

            MediaStream stream = null;

            if (mode == SubtitlePlaybackMode.None)
            {
                return null;
            }

            if (mode == SubtitlePlaybackMode.Default)
            {
                // Prefer embedded metadata over smart logic

                stream = streams.FirstOrDefault(s => s.IsForced && string.Equals(s.Language, audioTrackLanguage, StringComparison.OrdinalIgnoreCase)) ??
                    streams.FirstOrDefault(s => s.IsForced) ??
                    streams.FirstOrDefault(s => s.IsDefault);

                // if the audio language is not understood by the user, load their preferred subs, if there are any
                if (stream == null && !preferredLanguages.Contains(audioTrackLanguage, StringComparer.OrdinalIgnoreCase))
                {
                    stream = streams.Where(s => !s.IsForced).FirstOrDefault(s => preferredLanguages.Contains(s.Language, StringComparer.OrdinalIgnoreCase));
                }
            }
            else if (mode == SubtitlePlaybackMode.Smart)
            {
                // Prefer smart logic over embedded metadata

                // if the audio language is not understood by the user, load their preferred subs, if there are any
                if (!preferredLanguages.Contains(audioTrackLanguage, StringComparer.OrdinalIgnoreCase))
                {
                    stream = streams.Where(s => !s.IsForced).FirstOrDefault(s => preferredLanguages.Contains(s.Language, StringComparer.OrdinalIgnoreCase)) ??
                        streams.FirstOrDefault(s => preferredLanguages.Contains(s.Language, StringComparer.OrdinalIgnoreCase));
                }
            }
            else if (mode == SubtitlePlaybackMode.Always)
            {
                // always load the most suitable full subtitles
                stream = streams.FirstOrDefault(s => !s.IsForced);
            }
            else if (mode == SubtitlePlaybackMode.OnlyForced)
            {
                // always load the most suitable full subtitles
                stream = streams.FirstOrDefault(s => s.IsForced && string.Equals(s.Language, audioTrackLanguage, StringComparison.OrdinalIgnoreCase)) ??
                    streams.FirstOrDefault(s => s.IsForced);
            }

            // load forced subs if we have found no suitable full subtitles
            stream = stream ?? streams.FirstOrDefault(s => s.IsForced && string.Equals(s.Language, audioTrackLanguage, StringComparison.OrdinalIgnoreCase));

            if (stream != null)
            {
                return stream.Index;
            }

            return null;
        }

        private static IEnumerable<MediaStream> GetSortedStreams(IEnumerable<MediaStream> streams, MediaStreamType type, string[] languagePreferences)
        {
            // Give some preferance to external text subs for better performance
            return streams.Where(i => i.Type == type)
                .OrderBy(i =>
            {
                var index = FindIndex(languagePreferences, i.Language);

                return index == -1 ? 100 : index;
            })
                 .ThenBy(i => GetBooleanOrderBy(i.IsDefault))
                 .ThenBy(i => GetBooleanOrderBy(i.SupportsExternalStream))
                 .ThenBy(i => GetBooleanOrderBy(i.IsTextSubtitleStream))
                 .ThenBy(i => GetBooleanOrderBy(i.IsExternal))
                 .ThenBy(i => i.Index);
        }

        public static void SetSubtitleStreamScores(
            List<MediaStream> streams,
            string[] preferredLanguages,
            SubtitlePlaybackMode mode,
            string audioTrackLanguage)
        {
            if (mode == SubtitlePlaybackMode.None)
            {
                return;
            }

            streams = GetSortedStreams(streams, MediaStreamType.Subtitle, preferredLanguages)
                .ToList();

            var filteredStreams = new List<MediaStream>();

            if (mode == SubtitlePlaybackMode.Default)
            {
                // Prefer embedded metadata over smart logic
                filteredStreams = streams.Where(s => s.IsForced || s.IsDefault)
                    .ToList();
            }
            else if (mode == SubtitlePlaybackMode.Smart)
            {
                // Prefer smart logic over embedded metadata
                if (!preferredLanguages.Contains(audioTrackLanguage, StringComparer.OrdinalIgnoreCase))
                {
                    filteredStreams = streams.Where(s => !s.IsForced && preferredLanguages.Contains(s.Language, StringComparer.OrdinalIgnoreCase))
                        .ToList();
                }
            }
            else if (mode == SubtitlePlaybackMode.Always)
            {
                // always load the most suitable full subtitles
                filteredStreams = streams.Where(s => !s.IsForced)
                    .ToList();
            }
            else if (mode == SubtitlePlaybackMode.OnlyForced)
            {
                // always load the most suitable full subtitles
                filteredStreams = streams.Where(s => s.IsForced).ToList();
            }

            // load forced subs if we have found no suitable full subtitles
            if (filteredStreams.Count == 0)
            {
                filteredStreams = streams
                    .Where(s => s.IsForced && string.Equals(s.Language, audioTrackLanguage, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            foreach (var stream in filteredStreams)
            {
                stream.Score = GetSubtitleScore(stream, preferredLanguages);
            }
        }

        private static int FindIndex(string[] list, string value)
        {
            for (var i = 0; i < list.Length; i++)
            {
                if (string.Equals(list[i], value, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int GetSubtitleScore(MediaStream stream, string[] languagePreferences)
        {
            var values = new List<int>();

            var index = FindIndex(languagePreferences, stream.Language);

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
