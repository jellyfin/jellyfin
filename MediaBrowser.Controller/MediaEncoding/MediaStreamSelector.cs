using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Controller.MediaEncoding
{
    public static class MediaStreamSelector
    {
        public static int? GetDefaultAudioStreamIndex(List<MediaStream> streams, IEnumerable<string> preferredLanguages, bool preferDefaultTrack)
        {
            streams = GetSortedStreams(streams, MediaStreamType.Audio, preferredLanguages.ToList())
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

        public static int? GetDefaultSubtitleStreamIndex(List<MediaStream> streams,
            List<string> preferredLanguages,
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
                if (stream == null && !ContainsOrdinal(preferredLanguages, audioTrackLanguage))
                {
                    stream = streams.Where(s => !s.IsForced).FirstOrDefault(s => ContainsOrdinal(preferredLanguages, s.Language));
                }
            }
            else if (mode == SubtitlePlaybackMode.Smart)
            {
                // Prefer smart logic over embedded metadata

                // if the audio language is not understood by the user, load their preferred subs, if there are any
                if (!ContainsOrdinal(preferredLanguages, audioTrackLanguage))
                {
                    stream = streams.Where(s => !s.IsForced).FirstOrDefault(s => ContainsOrdinal(preferredLanguages, s.Language));
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

        private static bool ContainsOrdinal(IEnumerable<string> list, string item)
        {
            return list.Any(i => string.Equals(i, item, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<MediaStream> GetSortedStreams(IEnumerable<MediaStream> streams, MediaStreamType type, List<string> languagePreferences)
        {
            // Give some preferance to external text subs for better performance
            return streams.Where(i => i.Type == type)
                .OrderBy(i =>
            {
                var index = languagePreferences.FindIndex(l => string.Equals(i.Language, l, StringComparison.OrdinalIgnoreCase));

                return index == -1 ? 100 : index;
            })
                 .ThenBy(i => GetBooleanOrderBy(i.IsDefault))
                 .ThenBy(i => GetBooleanOrderBy(i.SupportsExternalStream))
                 .ThenBy(i => GetBooleanOrderBy(i.IsTextSubtitleStream))
                 .ThenBy(i => GetBooleanOrderBy(i.IsExternal))
                 .ThenBy(i => i.Index);
        }

        public static void SetSubtitleStreamScores(List<MediaStream> streams,
            List<string> preferredLanguages,
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

                // if the audio language is not understood by the user, load their preferred subs, if there are any
                if (!ContainsOrdinal(preferredLanguages, audioTrackLanguage))
                {
                    filteredStreams = streams.Where(s => !s.IsForced && ContainsOrdinal(preferredLanguages, s.Language))
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

        private static int GetSubtitleScore(MediaStream stream, List<string> languagePreferences)
        {
            var values = new List<int>();

            var index = languagePreferences.FindIndex(l => string.Equals(stream.Language, l, StringComparison.OrdinalIgnoreCase));

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
