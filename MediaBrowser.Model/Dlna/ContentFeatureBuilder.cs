#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.Model.Dlna
{
    public class ContentFeatureBuilder
    {
        public static string BuildImageHeader(
            DeviceProfile profile,
            string container,
            int? width,
            int? height,
            bool isDirectStream,
            string orgPn = null)
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

            if (string.IsNullOrEmpty(orgPn))
            {
                ResponseProfile mediaProfile = profile.GetImageMediaProfile(
                    container,
                    width,
                    height);

                orgPn = mediaProfile?.OrgPn;

                if (string.IsNullOrEmpty(orgPn))
                {
                    orgPn = GetImageOrgPnValue(container, width, height);
                }
            }

            if (string.IsNullOrEmpty(orgPn))
            {
                return orgOp.TrimStart(';') + orgCi + dlnaflags;
            }

            return "DLNA.ORG_PN=" + orgPn + orgOp + orgCi + dlnaflags;
        }

        public static string BuildAudioHeader(
            DeviceProfile profile,
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
            //     flagValue = flagValue | DlnaFlags.ByteBasedSeek;
            // }
            //  else if (runtimeTicks.HasValue)
            // {
            //     flagValue = flagValue | DlnaFlags.TimeBasedSeek;
            // }

            string dlnaflags = string.Format(
                CultureInfo.InvariantCulture,
                ";DLNA.ORG_FLAGS={0}",
                DlnaMaps.FlagsToString(flagValue));

            ResponseProfile mediaProfile = profile.GetAudioMediaProfile(
                container,
                audioCodec,
                audioChannels,
                audioBitrate,
                audioSampleRate,
                audioBitDepth);

            string orgPn = mediaProfile?.OrgPn;

            if (string.IsNullOrEmpty(orgPn))
            {
                orgPn = GetAudioOrgPnValue(container, audioBitrate, audioSampleRate, audioChannels);
            }

            if (string.IsNullOrEmpty(orgPn))
            {
                return orgOp.TrimStart(';') + orgCi + dlnaflags;
            }

            return "DLNA.ORG_PN=" + orgPn + orgOp + orgCi + dlnaflags;
        }

        public static IEnumerable<string> BuildVideoHeader(
            DeviceProfile profile,
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
            string videoRangeType,
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

            if (isDirectStream)
            {
                flagValue |= DlnaFlags.ByteBasedSeek;
            }

            // Time based seek is currently disabled when streaming. On LG CX3 adding DlnaFlags.TimeBasedSeek and orgPn causes the DLNA playback to fail (format not supported). Further investigations are needed before enabling the remaining code paths.
            //  else if (runtimeTicks.HasValue)
            // {
            //     flagValue = flagValue | DlnaFlags.TimeBasedSeek;
            // }

            string dlnaflags = string.Format(
                CultureInfo.InvariantCulture,
                ";DLNA.ORG_FLAGS={0}",
                DlnaMaps.FlagsToString(flagValue));

            ResponseProfile mediaProfile = profile.GetVideoMediaProfile(
                container,
                audioCodec,
                videoCodec,
                width,
                height,
                bitDepth,
                videoBitrate,
                videoProfile,
                videoRangeType,
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
                foreach (var s in GetVideoOrgPnValue(container, videoCodec, audioCodec, width, height, timestamp))
                {
                    orgPnValues.Add(s.ToString());
                    break;
                }
            }

            var contentFeatureList = new List<string>();

            foreach (string orgPn in orgPnValues)
            {
                if (string.IsNullOrEmpty(orgPn))
                {
                    contentFeatureList.Add(orgOp.TrimStart(';') + orgCi + dlnaflags);
                }
                else if (isDirectStream)
                {
                    // orgOp should be added all the time once the time based seek is resolved for transcoded streams
                    contentFeatureList.Add("DLNA.ORG_PN=" + orgPn + orgOp + orgCi + dlnaflags);
                }
                else
                {
                    contentFeatureList.Add("DLNA.ORG_PN=" + orgPn + orgCi + dlnaflags);
                }
            }

            if (orgPnValues.Count == 0)
            {
                contentFeatureList.Add(orgOp.TrimStart(';') + orgCi + dlnaflags);
            }

            return contentFeatureList;
        }

        private static string GetImageOrgPnValue(string container, int? width, int? height)
        {
            MediaFormatProfile? format = MediaFormatProfileResolver.ResolveImageFormat(container, width, height);

            return format.HasValue ? format.Value.ToString() : null;
        }

        private static string GetAudioOrgPnValue(string container, int? audioBitrate, int? audioSampleRate, int? audioChannels)
        {
            MediaFormatProfile? format = MediaFormatProfileResolver.ResolveAudioFormat(
                container,
                audioBitrate,
                audioSampleRate,
                audioChannels);

            return format.HasValue ? format.Value.ToString() : null;
        }

        private static MediaFormatProfile[] GetVideoOrgPnValue(string container, string videoCodec, string audioCodec, int? width, int? height, TransportStreamTimestamp timestamp)
        {
            return MediaFormatProfileResolver.ResolveVideoFormat(container, videoCodec, audioCodec, width, height, timestamp);
        }
    }
}
