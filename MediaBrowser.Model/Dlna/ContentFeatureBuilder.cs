using MediaBrowser.Model.MediaInfo;
using System.Collections.Generic;

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
            string orgOp = ";DLNA.ORG_OP=" + DlnaMaps.GetImageOrgOpValue();

            // 0 = native, 1 = transcoded
            const string orgCi = ";DLNA.ORG_CI=0";

            DlnaFlags flagValue = DlnaFlags.StreamingTransferMode |
                            DlnaFlags.BackgroundTransferMode |
                            DlnaFlags.DlnaV15;

            string dlnaflags = string.Format(";DLNA.ORG_FLAGS={0}",
             DlnaMaps.FlagsToString(flagValue));

            ResponseProfile mediaProfile = _profile.GetImageMediaProfile(container,
                width,
                height);

            string orgPn = mediaProfile == null ? null : mediaProfile.OrgPn;

            if (string.IsNullOrEmpty(orgPn))
            {
                orgPn = GetImageOrgPnValue(container, width, height);
            }

            string contentFeatures = string.IsNullOrEmpty(orgPn) ? string.Empty : "DLNA.ORG_PN=" + orgPn;

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
            string orgOp = ";DLNA.ORG_OP=" + DlnaMaps.GetOrgOpValue(runtimeTicks.HasValue, isDirectStream, transcodeSeekInfo);

            // 0 = native, 1 = transcoded
            string orgCi = isDirectStream ? ";DLNA.ORG_CI=0" : ";DLNA.ORG_CI=1";

            DlnaFlags flagValue = DlnaFlags.StreamingTransferMode |
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

            string dlnaflags = string.Format(";DLNA.ORG_FLAGS={0}",
             DlnaMaps.FlagsToString(flagValue));

            ResponseProfile mediaProfile = _profile.GetAudioMediaProfile(container,
                audioCodec,
                audioChannels,
                audioBitrate);

            string orgPn = mediaProfile == null ? null : mediaProfile.OrgPn;

            if (string.IsNullOrEmpty(orgPn))
            {
                orgPn = GetAudioOrgPnValue(container, audioBitrate, audioSampleRate, audioChannels);
            }

            string contentFeatures = string.IsNullOrEmpty(orgPn) ? string.Empty : "DLNA.ORG_PN=" + orgPn;

            return (contentFeatures + orgOp + orgCi + dlnaflags).Trim(';');
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
            float? videoFramerate,
            int? packetLength,
            TranscodeSeekInfo transcodeSeekInfo,
            bool? isAnamorphic)
        {
            // first bit means Time based seek supported, second byte range seek supported (not sure about the order now), so 01 = only byte seek, 10 = time based, 11 = both, 00 = none
            string orgOp = ";DLNA.ORG_OP=" + DlnaMaps.GetOrgOpValue(runtimeTicks.HasValue, isDirectStream, transcodeSeekInfo);

            // 0 = native, 1 = transcoded
            string orgCi = isDirectStream ? ";DLNA.ORG_CI=0" : ";DLNA.ORG_CI=1";

            DlnaFlags flagValue = DlnaFlags.StreamingTransferMode |
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

            string dlnaflags = string.Format(";DLNA.ORG_FLAGS={0}",
             DlnaMaps.FlagsToString(flagValue));

            ResponseProfile mediaProfile = _profile.GetVideoMediaProfile(container,
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
                timestamp,
                isAnamorphic);

            string orgPn = mediaProfile == null ? null : mediaProfile.OrgPn;

            if (string.IsNullOrEmpty(orgPn))
            {
                foreach (string s in GetVideoOrgPnValue(container, videoCodec, audioCodec, width, height, timestamp))
                {
                    orgPn = s;
                    break;
                }
            }

            if (string.IsNullOrEmpty(orgPn))
            {
                // TODO: Support multiple values and return multiple headers?
                foreach (string s in (orgPn ?? string.Empty).Split(','))
                {
                    orgPn = s;
                    break;
                }
            }

            string contentFeatures = string.IsNullOrEmpty(orgPn) ? string.Empty : "DLNA.ORG_PN=" + orgPn;

            return (contentFeatures + orgOp + orgCi + dlnaflags).Trim(';');
        }

        private string GetImageOrgPnValue(string container, int? width, int? height)
        {
            MediaFormatProfile? format = new MediaFormatProfileResolver()
                .ResolveImageFormat(container,
                width,
                height);

            return format.HasValue ? format.Value.ToString() : null;
        }

        private string GetAudioOrgPnValue(string container, int? audioBitrate, int? audioSampleRate, int? audioChannels)
        {
            MediaFormatProfile? format = new MediaFormatProfileResolver()
                .ResolveAudioFormat(container,
                audioBitrate,
                audioSampleRate,
                audioChannels);

            return format.HasValue ? format.Value.ToString() : null;
        }

        private List<string> GetVideoOrgPnValue(string container, string videoCodec, string audioCodec, int? width, int? height, TransportStreamTimestamp timestamp)
        {
            List<string> list = new List<string>();
            foreach (MediaFormatProfile i in new MediaFormatProfileResolver().ResolveVideoFormat(container, videoCodec, audioCodec, width, height, timestamp))
                list.Add(i.ToString());
            return list;
        }
    }
}
