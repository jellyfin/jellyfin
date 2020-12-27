using System;
using System.Collections.Generic;
using System.Globalization;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="ContentFeatureBuilder" />.
    /// </summary>
    public class ContentFeatureBuilder
    {
        private readonly DeviceProfile _profile;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentFeatureBuilder"/> class.
        /// </summary>
        /// <param name="profile">The profile<see cref="DeviceProfile"/>.</param>
        public ContentFeatureBuilder(DeviceProfile profile)
        {
            _profile = profile;
        }

        /// <summary>
        /// Builds an image header with the values provided.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="width">An optional width.</param>
        /// <param name="height">An optional height.</param>
        /// <param name="isDirectStream">True if it is a direct stream.</param>
        /// <param name="orgPn">An optional orgPn.</param>
        /// <returns>The image header as a string.</returns>
        public string BuildImageHeader(
            string container,
            int? width,
            int? height,
            bool isDirectStream,
            string? orgPn = null)
        {
            string orgOp = ";DLNA.ORG_OP=" + DlnaMaps.GetImageOrgOpValue();

            // 0 = native, 1 = transcoded
            var orgCi = isDirectStream ? ";DLNA.ORG_CI=0" : ";DLNA.ORG_CI=1";

            var flagValue = DlnaFlags.BackgroundTransferMode |
                            DlnaFlags.InteractiveTransferMode |
                            DlnaFlags.DlnaV15;

            string dlnaflags = string.Format(
                CultureInfo.InvariantCulture,
                ";DLNA.ORG_FLAGS={0}",
                DlnaMaps.FlagsToString(flagValue));

            ResponseProfile? mediaProfile = _profile.GetImageMediaProfile(
                container,
                width,
                height);

            if (string.IsNullOrEmpty(orgPn))
            {
                orgPn = mediaProfile?.OrgPn;
            }

            if (string.IsNullOrEmpty(orgPn))
            {
                orgPn = GetImageOrgPnValue(container, width, height);
            }

            string contentFeatures = string.IsNullOrEmpty(orgPn) ? string.Empty : "DLNA.ORG_PN=" + orgPn;

            return (contentFeatures + orgOp + orgCi + dlnaflags).Trim(';');
        }

        /// <summary>
        /// Builds an audio header from the values provided.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="audioCodec">The audio codec.</param>
        /// <param name="audioBitrate">An optional value indicating the audio bitrate.</param>
        /// <param name="audioSampleRate">An optional value indicating the audio sample rate.</param>
        /// <param name="audioChannels">An optional value indicating the number of audio channels.</param>
        /// <param name="audioBitDepth">An optional value indicating the audio bit depth.</param>
        /// <param name="isDirectStream">True if the container supports direct stream.</param>
        /// <param name="runtimeTicks">An optional value indicating the runtime ticks.</param>
        /// <param name="transcodeSeekInfo">The <see cref="TranscodeSeekInfo"/>.</param>
        /// <returns>The a string representing the audio header.</returns>
        public string BuildAudioHeader(
            string container,
            string audioCodec,
            int? audioBitrate,
            int? audioSampleRate,
            int? audioChannels,
            int? audioBitDepth,
            bool isDirectStream,
            long? runtimeTicks,
            TranscodeSeekInfo transcodeSeekInfo)
        {
            // first bit means Time based seek supported, second byte range seek supported (not sure about the order now), so 01 = only byte seek, 10 = time based, 11 = both, 00 = none
            string orgOp = ";DLNA.ORG_OP=" + DlnaMaps.GetOrgOpValue(runtimeTicks > 0, isDirectStream, transcodeSeekInfo);

            // 0 = native, 1 = transcoded
            string orgCi = isDirectStream ? ";DLNA.ORG_CI=0" : ";DLNA.ORG_CI=1";

            var flagValue = DlnaFlags.StreamingTransferMode |
                            DlnaFlags.BackgroundTransferMode |
                            DlnaFlags.InteractiveTransferMode |
                            DlnaFlags.DlnaV15;

            // if (isDirectStream)
            // {
            //    flagValue = flagValue | DlnaFlags.ByteBasedSeek;
            // }
            // else if (runtimeTicks.HasValue)
            // {
            //    flagValue = flagValue | DlnaFlags.TimeBasedSeek;
            // }

            string dlnaflags = string.Format(
                CultureInfo.InvariantCulture,
                ";DLNA.ORG_FLAGS={0}",
                DlnaMaps.FlagsToString(flagValue));

            ResponseProfile? mediaProfile = _profile.GetAudioMediaProfile(
                container,
                audioCodec,
                audioChannels,
                audioBitrate,
                audioSampleRate,
                audioBitDepth);

            string? orgPn = mediaProfile?.OrgPn;

            if (string.IsNullOrEmpty(orgPn))
            {
                orgPn = GetAudioOrgPnValue(container, audioBitrate, audioSampleRate, audioChannels);
            }

            string contentFeatures = string.IsNullOrEmpty(orgPn) ? string.Empty : "DLNA.ORG_PN=" + orgPn;

            return (contentFeatures + orgOp + orgCi + dlnaflags).Trim(';');
        }

        /// <summary>
        /// Builds the Video Header meeting the criteria.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="videoCodec">The videoCodec.</param>
        /// <param name="audioCodec">The audioCodec.</param>
        /// <param name="width">An optional value indicating the width.</param>
        /// <param name="height">An optional value indicating the height.</param>
        /// <param name="bitDepth">An optional value indicating the bit depth.</param>
        /// <param name="videoBitrate">An optional value indicating the video bitrate.</param>
        /// <param name="timestamp">The <see cref="TransportStreamTimestamp"/>.</param>
        /// <param name="isDirectStream">An optional value indicating the if is direct stream.</param>
        /// <param name="runtimeTicks">An optional value indicating the runtime ticks.</param>
        /// <param name="videoProfile">The video profile.</param>
        /// <param name="videoLevel">An optional value indicating the video level.</param>
        /// <param name="videoFramerate">An optional value indicating the video framerate.</param>
        /// <param name="packetLength">An optional value indicating the packet length.</param>
        /// <param name="transcodeSeekInfo">The <see cref="TranscodeSeekInfo"/>.</param>
        /// <param name="isAnamorphic">An optional value indicating the if it is Anamorphic.</param>
        /// <param name="isInterlaced">An optional value indicating the if it is Interlaced.</param>
        /// <param name="refFrames">An optional value indicating the ref frames.</param>
        /// <param name="numVideoStreams">An optional value indicating the number of video streams.</param>
        /// <param name="numAudioStreams">An optional value indicating the number of AudioStreams.</param>
        /// <param name="videoCodecTag">The video codec tag.</param>
        /// <param name="isAvc">An optional value indicating Avc.</param>
        /// <returns>An array of strings containing the video headers.</returns>
        public List<string> BuildVideoHeader(
            string container,
            string videoCodec,
            string audioCodec,
            int? width,
            int? height,
            int? bitDepth,
            int? videoBitrate,
            TransportStreamTimestamp timestamp,
            bool isDirectStream,
            long? runtimeTicks,
            string videoProfile,
            double? videoLevel,
            float? videoFramerate,
            int? packetLength,
            TranscodeSeekInfo transcodeSeekInfo,
            bool? isAnamorphic,
            bool? isInterlaced,
            int? refFrames,
            int? numVideoStreams,
            int? numAudioStreams,
            string videoCodecTag,
            bool? isAvc)
        {
            // first bit means Time based seek supported, second byte range seek supported (not sure about the order now), so 01 = only byte seek, 10 = time based, 11 = both, 00 = none
            string orgOp = ";DLNA.ORG_OP=" + DlnaMaps.GetOrgOpValue(runtimeTicks > 0, isDirectStream, transcodeSeekInfo);

            // 0 = native, 1 = transcoded
            string orgCi = isDirectStream ? ";DLNA.ORG_CI=0" : ";DLNA.ORG_CI=1";

            var flagValue = DlnaFlags.StreamingTransferMode |
                            DlnaFlags.BackgroundTransferMode |
                            DlnaFlags.InteractiveTransferMode |
                            DlnaFlags.DlnaV15;

            // if (isDirectStream)
            // {
            //    flagValue = flagValue | DlnaFlags.ByteBasedSeek;
            // }
            // else if (runtimeTicks.HasValue)
            // {
            //    flagValue = flagValue | DlnaFlags.TimeBasedSeek;
            // }

            string dlnaflags = string.Format(CultureInfo.InvariantCulture, ";DLNA.ORG_FLAGS={0}", DlnaMaps.FlagsToString(flagValue));

            ResponseProfile? mediaProfile = _profile.GetVideoMediaProfile(
                container,
                audioCodec,
                videoCodec,
                width,
                height,
                bitDepth,
                videoBitrate,
                videoProfile,
                videoLevel,
                videoFramerate,
                packetLength,
                timestamp,
                isAnamorphic,
                isInterlaced,
                refFrames,
                numVideoStreams,
                numAudioStreams,
                videoCodecTag,
                isAvc);

            var orgPnValues = new List<string>();

            if (mediaProfile != null && !string.IsNullOrEmpty(mediaProfile.OrgPn))
            {
                orgPnValues.AddRange(mediaProfile.OrgPn.Split(',', StringSplitOptions.RemoveEmptyEntries));
            }
            else
            {
                foreach (string s in GetVideoOrgPnValue(container, videoCodec, audioCodec, width, height, timestamp))
                {
                    orgPnValues.Add(s);
                    break;
                }
            }

            var contentFeatureList = new List<string>();

            foreach (string orgPn in orgPnValues)
            {
                string contentFeatures = string.IsNullOrEmpty(orgPn) ? string.Empty : "DLNA.ORG_PN=" + orgPn;

                var value = (contentFeatures + orgOp + orgCi + dlnaflags).Trim(';');

                contentFeatureList.Add(value);
            }

            if (orgPnValues.Count == 0)
            {
                string contentFeatures = string.Empty;

                var value = (contentFeatures + orgOp + orgCi + dlnaflags).Trim(';');

                contentFeatureList.Add(value);
            }

            return contentFeatureList;
        }

        private static string? GetImageOrgPnValue(string container, int? width, int? height)
        {
            MediaFormatProfile? format = MediaFormatProfileResolver.ResolveImageFormat(
                container,
                width,
                height);

            return format.HasValue ? format.Value.ToString() : null;
        }

        private static string? GetAudioOrgPnValue(string container, int? audioBitrate, int? audioSampleRate, int? audioChannels)
        {
            MediaFormatProfile? format = MediaFormatProfileResolver.ResolveAudioFormat(
                container,
                audioBitrate,
                audioSampleRate,
                audioChannels);

            return format.HasValue ? format.Value.ToString() : null;
        }

        private static string[] GetVideoOrgPnValue(string container, string videoCodec, string audioCodec, int? width, int? height, TransportStreamTimestamp timestamp)
        {
            return MediaFormatProfileResolver.ResolveVideoFormat(container, videoCodec, audioCodec, width, height, timestamp);
        }
    }
}
