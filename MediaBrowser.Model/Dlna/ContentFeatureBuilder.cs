using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.Model.Dlna
{
    public class ContentFeatureBuilder
    {
        private readonly DeviceProfile _profile;

        public ContentFeatureBuilder(DeviceProfile profile)
        {
            _profile = profile;
        }

        public string BuildImageHeader(string container,
            int? width,
            int? height)
        {
            var orgOp = ";DLNA.ORG_OP=" + DlnaMaps.GetImageOrgOpValue();

            // 0 = native, 1 = transcoded
            const string orgCi = ";DLNA.ORG_CI=0";

            var flagValue = DlnaFlags.StreamingTransferMode |
                            DlnaFlags.BackgroundTransferMode |
                            DlnaFlags.DlnaV15;

            var dlnaflags = string.Format(";DLNA.ORG_FLAGS={0}",
             FlagsToString(flagValue));

            var mediaProfile = _profile.GetImageMediaProfile(container,
                width,
                height);

            var orgPn = mediaProfile == null ? null : mediaProfile.OrgPn;

            if (string.IsNullOrEmpty(orgPn))
            {
                orgPn = GetImageOrgPnValue(container, width, height);
            }

            var contentFeatures = string.IsNullOrEmpty(orgPn) ? string.Empty : "DLNA.ORG_PN=" + orgPn;

            return (contentFeatures + orgOp + orgCi + dlnaflags).Trim(';');
        }

        public string BuildAudioHeader(string container,
            string audioCodec,
            int? audioBitrate,
            int? audioSampleRate,
            int? audioChannels,
            bool isDirectStream,
            long? runtimeTicks,
            TranscodeSeekInfo transcodeSeekInfo)
        {
            // first bit means Time based seek supported, second byte range seek supported (not sure about the order now), so 01 = only byte seek, 10 = time based, 11 = both, 00 = none
            var orgOp = ";DLNA.ORG_OP=" + DlnaMaps.GetOrgOpValue(runtimeTicks.HasValue, isDirectStream, transcodeSeekInfo);

            // 0 = native, 1 = transcoded
            var orgCi = isDirectStream ? ";DLNA.ORG_CI=0" : ";DLNA.ORG_CI=1";

            var flagValue = DlnaFlags.StreamingTransferMode |
                            DlnaFlags.BackgroundTransferMode |
                            DlnaFlags.DlnaV15;

            if (isDirectStream)
            {
                //flagValue = flagValue | DlnaFlags.DLNA_ORG_FLAG_BYTE_BASED_SEEK;
            }
            else if (runtimeTicks.HasValue)
            {
                //flagValue = flagValue | DlnaFlags.DLNA_ORG_FLAG_TIME_BASED_SEEK;
            }

            var dlnaflags = string.Format(";DLNA.ORG_FLAGS={0}",
             FlagsToString(flagValue));

            var mediaProfile = _profile.GetAudioMediaProfile(container,
                audioCodec,
                audioChannels,
                audioBitrate);

            var orgPn = mediaProfile == null ? null : mediaProfile.OrgPn;

            if (string.IsNullOrEmpty(orgPn))
            {
                orgPn = GetAudioOrgPnValue(container, audioBitrate, audioSampleRate, audioChannels);
            }

            var contentFeatures = string.IsNullOrEmpty(orgPn) ? string.Empty : "DLNA.ORG_PN=" + orgPn;

            return (contentFeatures + orgOp + orgCi + dlnaflags).Trim(';');
        }

        private static string FlagsToString(DlnaFlags flags)
        {
            //return Enum.Format(typeof(DlnaFlags), flags, "x");
            return string.Format("{0:X8}{1:D24}", (ulong)flags, 0);
        }

        public string BuildVideoHeader(string container,
            string videoCodec,
            string audioCodec,
            int? width,
            int? height,
            int? bitDepth,
            int? videoBitrate,
            int? audioChannels,
            int? audioBitrate,
            TransportStreamTimestamp timestamp,
            bool isDirectStream,
            long? runtimeTicks,
            string videoProfile,
            double? videoLevel,
            double? videoFramerate,
            int? packetLength,
            TranscodeSeekInfo transcodeSeekInfo)
        {
            // first bit means Time based seek supported, second byte range seek supported (not sure about the order now), so 01 = only byte seek, 10 = time based, 11 = both, 00 = none
            var orgOp = ";DLNA.ORG_OP=" + DlnaMaps.GetOrgOpValue(runtimeTicks.HasValue, isDirectStream, transcodeSeekInfo);

            // 0 = native, 1 = transcoded
            var orgCi = isDirectStream ? ";DLNA.ORG_CI=0" : ";DLNA.ORG_CI=1";

            var flagValue = DlnaFlags.StreamingTransferMode |
                            DlnaFlags.BackgroundTransferMode |
                            DlnaFlags.DlnaV15;

            if (isDirectStream)
            {
                //flagValue = flagValue | DlnaFlags.DLNA_ORG_FLAG_BYTE_BASED_SEEK;
            }
            else if (runtimeTicks.HasValue)
            {
                //flagValue = flagValue | DlnaFlags.DLNA_ORG_FLAG_TIME_BASED_SEEK;
            }

            var dlnaflags = string.Format(";DLNA.ORG_FLAGS={0}000000000000000000000000",
             Enum.Format(typeof(DlnaFlags), flagValue, "x"));

            var mediaProfile = _profile.GetVideoMediaProfile(container,
                audioCodec,
                videoCodec,
                audioBitrate,
                audioChannels,
                width,
                height,
                bitDepth,
                videoBitrate,
                videoProfile,
                videoLevel,
                videoFramerate,
                packetLength,
                timestamp);

            var orgPn = mediaProfile == null ? null : mediaProfile.OrgPn;

            if (string.IsNullOrEmpty(orgPn))
            {
                orgPn = GetVideoOrgPnValue(container, videoCodec, audioCodec, width, height, timestamp)
                    .FirstOrDefault();

                // TODO: Support multiple values and return multiple headers?
                orgPn = (orgPn ?? string.Empty).Split(',').FirstOrDefault();
            }

            var contentFeatures = string.IsNullOrEmpty(orgPn) ? string.Empty : "DLNA.ORG_PN=" + orgPn;

            return (contentFeatures + orgOp + orgCi + dlnaflags).Trim(';');
        }

        private string GetImageOrgPnValue(string container, int? width, int? height)
        {
            var format = new MediaFormatProfileResolver()
                .ResolveImageFormat(container,
                width,
                height);

            return format.HasValue ? format.Value.ToString() : null;
        }

        private string GetAudioOrgPnValue(string container, int? audioBitrate, int? audioSampleRate, int? audioChannels)
        {
            var format = new MediaFormatProfileResolver()
                .ResolveAudioFormat(container,
                audioBitrate,
                audioSampleRate,
                audioChannels);

            return format.HasValue ? format.Value.ToString() : null;
        }

        private IEnumerable<string> GetVideoOrgPnValue(string container, string videoCodec, string audioCodec, int? width, int? height, TransportStreamTimestamp timestamp)
        {
            return new MediaFormatProfileResolver()
                .ResolveVideoFormat(container,
                    videoCodec,
                    audioCodec,
                    width,
                    height,
                    timestamp)
                    .Select(i => i.ToString());
        }
    }
}
