using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Model.MediaInfo
{
    public static class MediaStreamSelector
    {
        public static int? GetDefaultAudioStreamIndex(MediaSourceInfo mediaSource, IEnumerable<string> preferredLanguages, bool preferDefaultTrack)
        {
            var streams = GetSortedStreams(mediaSource.MediaStreams, MediaStreamType.Audio, preferredLanguages.FirstOrDefault())
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

        public static int? GetDefaultSubtitleStreamIndex(MediaSourceInfo mediaSource, 
            IEnumerable<string> preferredLanguages, 
            SubtitlePlaybackMode mode,
            string audioTrackLanguage)
        {
            var streams = GetSortedStreams(mediaSource.MediaStreams, MediaStreamType.Subtitle, preferredLanguages.FirstOrDefault())
                .ToList();

            MediaStream stream = null;

            if (mode == SubtitlePlaybackMode.Default)
            {
                stream = streams.FirstOrDefault(i => i.IsDefault);
            }
            else if (mode == SubtitlePlaybackMode.Always)
            {
                stream = streams.FirstOrDefault(i => i.IsDefault) ??
                    streams.FirstOrDefault();
            }
            else if (mode == SubtitlePlaybackMode.OnlyForced)
            {
                stream = streams.FirstOrDefault(i => i.IsForced);
            }

            if (stream != null)
            {
                return stream.Index;
            }

            return null;
        }
        
        private static IEnumerable<MediaStream> GetSortedStreams(IEnumerable<MediaStream> streams, MediaStreamType type, string defaultLanguage)
        {
            var orderStreams = streams
                .Where(i => i.Type == type);

            // For subs give a preference to text for performance

            if (string.IsNullOrEmpty(defaultLanguage))
            {
                return orderStreams.OrderBy(i => i.IsDefault)
                    .ThenBy(i => !i.IsGraphicalSubtitleStream)
                    .ThenBy(i => i.Index)
                    .ToList();
            }

            return orderStreams.OrderBy(i => string.Equals(i.Language, defaultLanguage, StringComparison.OrdinalIgnoreCase))
                .ThenBy(i => i.IsDefault)
                .ThenBy(i => !i.IsGraphicalSubtitleStream)
                .ThenBy(i => i.Index)
                .ToList();
        }
    }
}
