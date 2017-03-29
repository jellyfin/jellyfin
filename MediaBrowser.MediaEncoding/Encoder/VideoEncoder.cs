using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Model.Diagnostics;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class VideoEncoder : BaseEncoder
    {
        public VideoEncoder(MediaEncoder mediaEncoder, ILogger logger, IServerConfigurationManager configurationManager, IFileSystem fileSystem, IIsoManager isoManager, ILibraryManager libraryManager, ISessionManager sessionManager, ISubtitleEncoder subtitleEncoder, IMediaSourceManager mediaSourceManager, IProcessFactory processFactory) : base(mediaEncoder, logger, configurationManager, fileSystem, isoManager, libraryManager, sessionManager, subtitleEncoder, mediaSourceManager, processFactory)
        {
        }

        protected override string GetCommandLineArguments(EncodingJob state)
        {
            // Get the output codec name
            var encodingOptions = GetEncodingOptions();

            return EncodingHelper.GetProgressiveVideoFullCommandLine(state, encodingOptions, state.OutputFilePath, "superfast");
        }

        protected override string GetOutputFileExtension(EncodingJob state)
        {
            var ext = base.GetOutputFileExtension(state);

            if (!string.IsNullOrEmpty(ext))
            {
                return ext;
            }

            var videoCodec = state.Options.VideoCodec;

            if (string.Equals(videoCodec, "h264", StringComparison.OrdinalIgnoreCase))
            {
                return ".ts";
            }
            if (string.Equals(videoCodec, "theora", StringComparison.OrdinalIgnoreCase))
            {
                return ".ogv";
            }
            if (string.Equals(videoCodec, "vpx", StringComparison.OrdinalIgnoreCase))
            {
                return ".webm";
            }
            if (string.Equals(videoCodec, "wmv", StringComparison.OrdinalIgnoreCase))
            {
                return ".asf";
            }

            return null;
        }

        protected override bool IsVideoEncoder
        {
            get { return true; }
        }

    }
}