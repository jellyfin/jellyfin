using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using System;
using MediaBrowser.Model.Diagnostics;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class AudioEncoder : BaseEncoder
    {
        public AudioEncoder(MediaEncoder mediaEncoder, ILogger logger, IServerConfigurationManager configurationManager, IFileSystem fileSystem, IIsoManager isoManager, ILibraryManager libraryManager, ISessionManager sessionManager, ISubtitleEncoder subtitleEncoder, IMediaSourceManager mediaSourceManager, IProcessFactory processFactory) : base(mediaEncoder, logger, configurationManager, fileSystem, isoManager, libraryManager, sessionManager, subtitleEncoder, mediaSourceManager, processFactory)
        {
        }

        protected override string GetCommandLineArguments(EncodingJob state)
        {
            var encodingOptions = GetEncodingOptions();

            return EncodingHelper.GetProgressiveAudioFullCommandLine(state, encodingOptions, state.OutputFilePath);
        }

        protected override string GetOutputFileExtension(EncodingJob state)
        {
            var ext = base.GetOutputFileExtension(state);

            if (!string.IsNullOrEmpty(ext))
            {
                return ext;
            }

            var audioCodec = state.Options.AudioCodec;

            if (string.Equals("aac", audioCodec, StringComparison.OrdinalIgnoreCase))
            {
                return ".aac";
            }
            if (string.Equals("mp3", audioCodec, StringComparison.OrdinalIgnoreCase))
            {
                return ".mp3";
            }
            if (string.Equals("vorbis", audioCodec, StringComparison.OrdinalIgnoreCase))
            {
                return ".ogg";
            }
            if (string.Equals("wma", audioCodec, StringComparison.OrdinalIgnoreCase))
            {
                return ".wma";
            }

            return null;
        }

        protected override bool IsVideoEncoder
        {
            get { return false; }
        }

    }
}
