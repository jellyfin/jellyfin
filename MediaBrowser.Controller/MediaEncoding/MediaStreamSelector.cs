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
            streams = GetSortedStreams(streams, MediaStreamType.Subtitle, preferredLanguages).ToList();

            var full = streams.Where(s => !s.IsForced);

            MediaStream stream = null;

            if (mode == SubtitlePlaybackMode.None)
            {
                return null;
            }

            if (mode == SubtitlePlaybackMode.Default)
            {
                // if the audio language is not understood by the user, load their preferred subs, if there are any
                if (!ContainsOrdinal(preferredLanguages, audioTrackLanguage))
                {
                    stream = full.FirstOrDefault(s => ContainsOrdinal(preferredLanguages, s.Language));
                }
            }
            else if (mode == SubtitlePlaybackMode.Always)
            {
                // always load the most suitable full subtitles
                stream = full.FirstOrDefault();
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
            var orderStreams = streams
                .Where(i => i.Type == type);

            // Give some preferance to external text subs for better performance
            return orderStreams.OrderBy(i =>
            {
                var index = languagePreferences.FindIndex(l => string.Equals(i.Language, l, StringComparison.OrdinalIgnoreCase));

                return index == -1 ? 100 : index;
            })
                 .ThenBy(i => i.IsDefault)
                 .ThenBy(i => i.IsTextSubtitleStream)
                 .ThenBy(i => i.IsExternal)
                 .ThenBy(i => i.Index)
                 .ToList();
        }
    }
}
