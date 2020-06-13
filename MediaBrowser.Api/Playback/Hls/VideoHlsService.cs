using System;
using System.Globalization;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.Playback.Hls
{
    [Route("/Videos/{Id}/live.m3u8", "GET")]
    public class GetLiveHlsStream : VideoStreamRequest
    {
    }

    /// <summary>
    /// Class VideoHlsService
    /// </summary>
    [Authenticated]
    public class VideoHlsService : BaseHlsService
    {
        public VideoHlsService(
            ILogger<VideoHlsService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IUserManager userManager,
            ILibraryManager libraryManager,
            IIsoManager isoManager,
            IMediaEncoder mediaEncoder,
            IFileSystem fileSystem,
            IDlnaManager dlnaManager,
            IDeviceManager deviceManager,
            IMediaSourceManager mediaSourceManager,
            IJsonSerializer jsonSerializer,
            IAuthorizationContext authorizationContext,
            EncodingHelper encodingHelper)
            : base(
                logger,
                serverConfigurationManager,
                httpResultFactory,
                userManager,
                libraryManager,
                isoManager,
                mediaEncoder,
                fileSystem,
                dlnaManager,
                deviceManager,
                mediaSourceManager,
                jsonSerializer,
                authorizationContext,
                encodingHelper)
        {
        }

        public Task<object> Get(GetLiveHlsStream request)
        {
            return ProcessRequestAsync(request, true);
        }

        /// <summary>
        /// Gets the audio arguments.
        /// </summary>
        protected override string GetAudioArguments(StreamState state, EncodingOptions encodingOptions)
        {
            var codec = EncodingHelper.GetAudioEncoder(state);

            if (EncodingHelper.IsCopyCodec(codec))
            {
                return "-codec:a:0 copy";
            }

            var args = "-codec:a:0 " + codec;

            var channels = state.OutputAudioChannels;

            if (channels.HasValue)
            {
                args += " -ac " + channels.Value;
            }

            var bitrate = state.OutputAudioBitrate;

            if (bitrate.HasValue)
            {
                args += " -ab " + bitrate.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (state.OutputAudioSampleRate.HasValue)
            {
                args += " -ar " + state.OutputAudioSampleRate.Value.ToString(CultureInfo.InvariantCulture);
            }

            args += " " + EncodingHelper.GetAudioFilterParam(state, encodingOptions, true);

            return args;
        }

        /// <summary>
        /// Gets the video arguments.
        /// </summary>
        protected override string GetVideoArguments(StreamState state, EncodingOptions encodingOptions)
        {
            if (!state.IsOutputVideo)
            {
                return string.Empty;
            }

            var codec = EncodingHelper.GetVideoEncoder(state, encodingOptions);

            var args = "-codec:v:0 " + codec;

            // if (state.EnableMpegtsM2TsMode)
            // {
            //     args += " -mpegts_m2ts_mode 1";
            // }

            // See if we can save come cpu cycles by avoiding encoding
            if (codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                // if h264_mp4toannexb is ever added, do not use it for live tv
                if (state.VideoStream != null &&
                    !string.Equals(state.VideoStream.NalLengthSize, "0", StringComparison.OrdinalIgnoreCase))
                {
                    string bitStreamArgs = EncodingHelper.GetBitStreamArgs(state.VideoStream);
                    if (!string.IsNullOrEmpty(bitStreamArgs))
                    {
                        args += " " + bitStreamArgs;
                    }
                }
            }
            else
            {
                var keyFrameArg = string.Format(" -force_key_frames \"expr:gte(t,n_forced*{0})\"",
                    state.SegmentLength.ToString(CultureInfo.InvariantCulture));

                var hasGraphicalSubs = state.SubtitleStream != null && !state.SubtitleStream.IsTextSubtitleStream && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode;

                args += " " + EncodingHelper.GetVideoQualityParam(state, codec, encodingOptions, GetDefaultEncoderPreset()) + keyFrameArg;

                // Add resolution params, if specified
                if (!hasGraphicalSubs)
                {
                    args += EncodingHelper.GetOutputSizeParam(state, encodingOptions, codec);
                }

                // This is for internal graphical subs
                if (hasGraphicalSubs)
                {
                    args += EncodingHelper.GetGraphicalSubtitleParam(state, encodingOptions, codec);
                }
            }

            args += " -flags -global_header";

            if (!string.IsNullOrEmpty(state.OutputVideoSync))
            {
                args += " -vsync " + state.OutputVideoSync;
            }

            args += EncodingHelper.GetOutputFFlags(state);

            return args;
        }
    }
}
