#pragma warning disable CS1591

using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Entities;

namespace Emby.Server.Implementations.Library
{
    /// <summary>
    /// Filters ffprobe results for live TV playback.
    /// </summary>
    public static class LiveStreamMediaStreamFilter
    {
        /// <summary>
        /// Retains data streams (for ffmpeg index alignment), the first video and audio stream,
        /// and selectable subtitle streams (e.g. DVBSUB). DVB teletext is excluded.
        /// Stream indices and metadata are preserved.
        /// </summary>
        /// <param name="mediaStreams">The probed media streams.</param>
        /// <returns>The filtered stream list.</returns>
        public static IReadOnlyList<MediaStream> FilterProbedStreams(IEnumerable<MediaStream> mediaStreams)
        {
            var streams = mediaStreams as IReadOnlyList<MediaStream> ?? mediaStreams.ToList();

            var filtered = new List<MediaStream>();
            filtered.AddRange(streams.Where(i => i.Type == MediaStreamType.Data));
            filtered.AddRange(streams.Where(i => i.Type == MediaStreamType.Video).Take(1));
            filtered.AddRange(streams.Where(i =>
                i.Type == MediaStreamType.Audio
                && (!i.Channels.HasValue || i.Channels.Value > 0)).Take(1));
            filtered.AddRange(streams.Where(i =>
                i.Type == MediaStreamType.Subtitle
                && !MediaStream.IsTeletextFormat(i.Codec ?? string.Empty)));

            return filtered;
        }
    }
}
