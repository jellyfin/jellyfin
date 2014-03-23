using MediaBrowser.Controller.Dlna;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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
            var dlnaCommand = BuildDlnaUrl(item.DeviceProfileName, item.MediaSourceId, deviceProperties.UUID, !item.Transcode, null, item.AudioCodec, item.AudioStreamIndex, item.SubtitleStreamIndex, null, 128000, item.StartPositionTicks, item.TranscodingSettings);

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
            var dlnaCommand = BuildDlnaUrl(item.DeviceProfileName, item.MediaSourceId, deviceProperties.UUID, !item.Transcode, item.VideoCodec, item.AudioCodec, item.AudioStreamIndex, item.SubtitleStreamIndex, 1500000, 128000, item.StartPositionTicks, item.TranscodingSettings);

            return string.Format("{0}/Videos/{1}/stream{2}?{3}", serverAddress, item.ItemId, item.Container, dlnaCommand);
        }

        /// <summary>
        /// Builds the dlna URL.
        /// </summary>
        private static string BuildDlnaUrl(string deviceProfileName, string mediaSourceId, string deviceID, bool isStatic, string videoCodec, string audioCodec, int? audiostreamIndex, int? subtitleIndex, int? videoBitrate, int? audioBitrate, long? startPositionTicks, List<TranscodingSetting> settings)
        {
            var profile = settings.Where(i => i.Name == TranscodingSettingType.VideoProfile).Select(i => i.Value).FirstOrDefault();
            var videoLevel = settings.Where(i => i.Name == TranscodingSettingType.VideoLevel).Select(i => i.Value).FirstOrDefault();
            var maxAudioChannels = settings.Where(i => i.Name == TranscodingSettingType.MaxAudioChannels).Select(i => i.Value).FirstOrDefault();

            var usCulture = new CultureInfo("en-US");

            var list = new List<string>
            {
                deviceProfileName ?? string.Empty,
                deviceID ?? string.Empty,
                mediaSourceId ?? string.Empty,
                isStatic.ToString().ToLower(),
                videoCodec ?? string.Empty,
                audioCodec ?? string.Empty,
                audiostreamIndex.HasValue ? audiostreamIndex.Value.ToString(usCulture) : string.Empty,
                subtitleIndex.HasValue ? subtitleIndex.Value.ToString(usCulture) : string.Empty,
                videoBitrate.HasValue ? videoBitrate.Value.ToString(usCulture) : string.Empty,
                audioBitrate.HasValue ? audioBitrate.Value.ToString(usCulture) : string.Empty,
                maxAudioChannels ?? string.Empty,
                startPositionTicks.HasValue ? startPositionTicks.Value.ToString(usCulture) : string.Empty,
                profile ?? string.Empty,
                videoLevel ?? string.Empty
            };

            return string.Format("Params={0}", string.Join(";", list.ToArray()));
        }
    }
}