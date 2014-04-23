using System;

namespace MediaBrowser.Model.Dlna
{
    public class ContentFeatureBuilder
    {
        private readonly DeviceProfile _profile;

        public ContentFeatureBuilder(DeviceProfile profile)
        {
            _profile = profile;
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

            var dlnaflags = string.Format(";DLNA.ORG_FLAGS={0}000000000000000000000000",
             Enum.Format(typeof(DlnaFlags), flagValue, "x"));

            var mediaProfile = _profile.GetAudioMediaProfile(container, audioCodec);

            var orgPn = mediaProfile == null ? null : mediaProfile.OrgPn;

            if (string.IsNullOrEmpty(orgPn))
            {
                orgPn = GetAudioOrgPnValue(container, audioBitrate, audioSampleRate, audioChannels);
            }

            var contentFeatures = string.IsNullOrEmpty(orgPn) ? string.Empty : "DLNA.ORG_PN=" + orgPn;

            return (contentFeatures + orgOp + orgCi + dlnaflags).Trim(';');
        }

        public string BuildVideoHeader(string container, 
            string videoCodec, 
            string audioCodec, 
            int? width, 
            int? height, 
            int? bitrate, 
            TransportStreamTimestamp timestamp, 
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

            var dlnaflags = string.Format(";DLNA.ORG_FLAGS={0}000000000000000000000000",
             Enum.Format(typeof(DlnaFlags), flagValue, "x"));

            var mediaProfile = _profile.GetVideoMediaProfile(container, audioCodec, videoCodec);
            var orgPn = mediaProfile == null ? null : mediaProfile.OrgPn;
            
            if (string.IsNullOrEmpty(orgPn))
            {
                orgPn = GetVideoOrgPnValue(container, videoCodec, audioCodec, width, height, bitrate, timestamp);
            }

            var contentFeatures = string.IsNullOrEmpty(orgPn) ? string.Empty : "DLNA.ORG_PN=" + orgPn;

            return (contentFeatures + orgOp + orgCi + dlnaflags).Trim(';');
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

        private string GetVideoOrgPnValue(string container, string videoCodec, string audioCodec, int? width, int? height, int? bitrate, TransportStreamTimestamp timestamp)
        {
            var videoFormat = new MediaFormatProfileResolver()
                .ResolveVideoFormat(container,
                    videoCodec,
                    audioCodec,
                    width,
                    height,
                    bitrate,
                    timestamp);

            return videoFormat.HasValue ? videoFormat.Value.ToString() : null;
        }
    }
}
