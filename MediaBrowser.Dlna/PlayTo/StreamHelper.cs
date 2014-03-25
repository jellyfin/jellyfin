using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.Globalization;

namespace MediaBrowser.Dlna.PlayTo
{
    class StreamHelper
    {
        /// <summary>
        /// Gets the audio URL.
        /// </summary>
        /// <param name="deviceProperties">The device properties.</param>
        /// <param name="item">The item.</param>
        /// <param name="streams">The streams.</param>
        /// <param name="serverAddress">The server address.</param>
        /// <returns>System.String.</returns>
        internal static string GetAudioUrl(DeviceInfo deviceProperties, PlaylistItem item, List<MediaStream> streams, string serverAddress)
        {
            var dlnaCommand = BuildDlnaUrl(deviceProperties, item);

            return string.Format("{0}/audio/{1}/stream{2}?{3}", serverAddress, item.ItemId, "." + item.Container.TrimStart('.'), dlnaCommand);
        }

        /// <summary>
        /// Gets the video URL.
        /// </summary>
        /// <param name="deviceProperties">The device properties.</param>
        /// <param name="item">The item.</param>
        /// <param name="streams">The streams.</param>
        /// <param name="serverAddress">The server address.</param>
        /// <returns>The url to send to the device</returns>
        internal static string GetVideoUrl(DeviceInfo deviceProperties, PlaylistItem item, List<MediaStream> streams, string serverAddress)
        {
            var dlnaCommand = BuildDlnaUrl(deviceProperties, item);

            return string.Format("{0}/Videos/{1}/stream{2}?{3}", serverAddress, item.ItemId, item.Container, dlnaCommand);
        }

        /// <summary>
        /// Builds the dlna URL.
        /// </summary>
        private static string BuildDlnaUrl(DeviceInfo deviceProperties, PlaylistItem item)
        {
            var usCulture = new CultureInfo("en-US");
            
            var list = new List<string>
            {
                deviceProperties.UUID ?? string.Empty,
                item.MediaSourceId ?? string.Empty,
                (!item.Transcode).ToString().ToLower(),
                item.VideoCodec ?? string.Empty,
                item.AudioCodec ?? string.Empty,
                item.AudioStreamIndex.HasValue ? item.AudioStreamIndex.Value.ToString(usCulture) : string.Empty,
                item.SubtitleStreamIndex.HasValue ? item.SubtitleStreamIndex.Value.ToString(usCulture) : string.Empty,
                item.VideoBitrate.HasValue ? item.VideoBitrate.Value.ToString(usCulture) : string.Empty,
                item.AudioBitrate.HasValue ? item.AudioBitrate.Value.ToString(usCulture) : string.Empty,
                item.MaxAudioChannels.HasValue ? item.MaxAudioChannels.Value.ToString(usCulture) : string.Empty,
                item.MaxFramerate.HasValue ? item.MaxFramerate.Value.ToString(usCulture) : string.Empty,
                item.MaxWidth.HasValue ? item.MaxWidth.Value.ToString(usCulture) : string.Empty,
                item.MaxHeight.HasValue ? item.MaxHeight.Value.ToString(usCulture) : string.Empty,
                item.StartPositionTicks.ToString(usCulture),
                item.VideoLevel.HasValue ? item.VideoLevel.Value.ToString(usCulture) : string.Empty
            };

            return string.Format("Params={0}", string.Join(";", list.ToArray()));
        }
    }
}