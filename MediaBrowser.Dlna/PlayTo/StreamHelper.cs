using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MediaBrowser.Dlna.PlayTo
{
    class StreamHelper
    {
        /// <summary>
        /// Gets the dlna headers.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        internal static string GetDlnaHeaders(PlaylistItem item)
        {
            var orgOp = item.Transcode ? ";DLNA.ORG_OP=00" : ";DLNA.ORG_OP=01";

            var orgCi = item.Transcode ? ";DLNA.ORG_CI=0" : ";DLNA.ORG_CI=1";

            const string dlnaflags = ";DLNA.ORG_FLAGS=01500000000000000000000000000000";

            var contentFeatures = string.Empty;

            if (string.Equals(item.FileFormat, "mp3", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=MP3";
            }
            else if (string.Equals(item.FileFormat, "wma", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=WMABASE";
            }
            else if (string.Equals(item.FileFormat, "wmw", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=WMVMED_BASE";
            }
            else if (string.Equals(item.FileFormat, "asf", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=WMVMED_BASE";
            }
            else if (string.Equals(item.FileFormat, "avi", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=AVI";
            }
            else if (string.Equals(item.FileFormat, "mkv", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=MATROSKA";
            }
            else if (string.Equals(item.FileFormat, "mp4", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=AVC_MP4_MP_HD_720p_AAC";
            }
            else if (string.Equals(item.FileFormat, "mpeg", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=MPEG_PS_PAL";
            }
            else if (string.Equals(item.FileFormat, "ts", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=MPEG_PS_PAL";
            }
            else if (item.IsVideo)
            {
                //Default to AVI for video
                contentFeatures = "DLNA.ORG_PN=AVI";
            }
            else
            {
                //Default to MP3 for audio
                contentFeatures = "DLNA.ORG_PN=MP3";
            }

            return (contentFeatures + orgOp + orgCi + dlnaflags).Trim(';');
        }

        #region Audio

        /// <summary>
        /// Gets the audio URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="serverAddress">The server address.</param>
        /// <returns>System.String.</returns>
        internal static string GetAudioUrl(PlaylistItem item, string serverAddress)
        {
            if (!item.Transcode)
                return string.Format("{0}/audio/{1}/stream{2}?Static=True", serverAddress, item.ItemId, item.FileFormat);

            return string.Format("{0}/audio/{1}/stream.mp3?AudioCodec=Mp3", serverAddress, item.ItemId);
        }

        #endregion

        #region Video

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
            string dlnaCommand = string.Empty;
            if (!item.Transcode)
            {
                dlnaCommand = BuildDlnaUrl(deviceProperties.UUID, !item.Transcode, null, null, null, null, null, null, null, null, null, null, item.MimeType);
                return string.Format("{0}/Videos/{1}/stream{2}?{3}", serverAddress, item.ItemId, item.FileFormat, dlnaCommand);
            }
            var videostream = streams.Where(m => m.Type == MediaStreamType.Video).OrderBy(m => m.IsDefault).FirstOrDefault();
            var audiostream = streams.Where(m => m.Type == MediaStreamType.Audio).OrderBy(m => m.IsDefault).FirstOrDefault();

            var videoCodec = GetVideoCodec(videostream);
            var audioCodec = GetAudioCodec(audiostream);
            int? videoBitrate = null;
            int? audioBitrate = null;
            int? audioChannels = null;

            if (videoCodec != VideoCodecs.Copy)
                videoBitrate = 2000000;

            if (audioCodec != AudioCodecs.Copy)
            {
                audioBitrate = 128000;
                audioChannels = 2;
            }

            dlnaCommand = BuildDlnaUrl(deviceProperties.UUID, !item.Transcode, videoCodec, audioCodec, null, null, videoBitrate, audioChannels, audioBitrate, item.StartPositionTicks, "baseline", "3", item.MimeType);
            return string.Format("{0}/Videos/{1}/stream{2}?{3}", serverAddress, item.ItemId, item.FileFormat, dlnaCommand);
        }

        /// <summary>
        /// Gets the video codec.
        /// </summary>
        /// <param name="videoStream">The video stream.</param>
        /// <returns></returns>
        private static VideoCodecs GetVideoCodec(MediaStream videoStream)
        {
            switch (videoStream.Codec.ToLower())
            {
                case "h264":
                case "mpeg4":
                    return VideoCodecs.Copy;

            }
            return VideoCodecs.H264;
        }

        /// <summary>
        /// Gets the audio codec.
        /// </summary>
        /// <param name="audioStream">The audio stream.</param>
        /// <returns></returns>
        private static AudioCodecs GetAudioCodec(MediaStream audioStream)
        {
            if (audioStream != null)
            {
                switch (audioStream.Codec.ToLower())
                {
                    case "aac":
                    case "mp3":
                    case "wma":
                        return AudioCodecs.Copy;

                }
            }
            return AudioCodecs.Aac;
        }

        /// <summary>
        /// Builds the dlna URL.
        /// </summary>
        private static string BuildDlnaUrl(string deviceID, bool isStatic, VideoCodecs? videoCodec, AudioCodecs? audioCodec, int? subtitleIndex, int? audiostreamIndex, int? videoBitrate, int? audiochannels, int? audioBitrate, long? startPositionTicks, string profile, string videoLevel, string mimeType)
        {
            var usCulture = new CultureInfo("en-US");

            var dlnaparam = string.Format("Params={0};", deviceID);
            dlnaparam += isStatic ? "true;" : "false;";
            dlnaparam += videoCodec.HasValue ? videoCodec.Value + ";" : ";";
            dlnaparam += audioCodec.HasValue ? audioCodec.Value + ";" : ";";
            dlnaparam += audiostreamIndex.HasValue ? audiostreamIndex.Value.ToString(usCulture) + ";" : ";";
            dlnaparam += subtitleIndex.HasValue ? subtitleIndex.Value.ToString(usCulture) + ";" : ";";
            dlnaparam += videoBitrate.HasValue ? videoBitrate.Value.ToString(usCulture) + ";" : ";";
            dlnaparam += audioBitrate.HasValue ? audioBitrate.Value.ToString(usCulture) + ";" : ";";
            dlnaparam += audiochannels.HasValue ? audiochannels.Value.ToString(usCulture) + ";" : ";";
            dlnaparam += startPositionTicks.HasValue ? startPositionTicks.Value.ToString(usCulture) + ";" : ";";
            dlnaparam += profile + ";";
            dlnaparam += videoLevel + ";";
            dlnaparam += mimeType + ";";

            return dlnaparam;
        }

        #endregion

    }
}