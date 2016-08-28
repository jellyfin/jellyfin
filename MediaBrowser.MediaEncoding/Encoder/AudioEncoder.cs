using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class AudioEncoder : BaseEncoder
    {
        public AudioEncoder(MediaEncoder mediaEncoder, ILogger logger, IServerConfigurationManager configurationManager, IFileSystem fileSystem, IIsoManager isoManager, ILibraryManager libraryManager, ISessionManager sessionManager, ISubtitleEncoder subtitleEncoder, IMediaSourceManager mediaSourceManager) : base(mediaEncoder, logger, configurationManager, fileSystem, isoManager, libraryManager, sessionManager, subtitleEncoder, mediaSourceManager)
        {
        }

        protected override Task<string> GetCommandLineArguments(EncodingJob state)
        {
            var audioTranscodeParams = new List<string>();

            var bitrate = state.OutputAudioBitrate;

            if (bitrate.HasValue)
            {
                audioTranscodeParams.Add("-ab " + bitrate.Value.ToString(UsCulture));
            }

            if (state.OutputAudioChannels.HasValue)
            {
                audioTranscodeParams.Add("-ac " + state.OutputAudioChannels.Value.ToString(UsCulture));
            }

            // opus will fail on 44100
            if (!string.Equals(state.OutputAudioCodec, "opus", StringComparison.OrdinalIgnoreCase))
            {
                if (state.OutputAudioSampleRate.HasValue)
                {
                    audioTranscodeParams.Add("-ar " + state.OutputAudioSampleRate.Value.ToString(UsCulture));
                }
            }

            var threads = GetNumberOfThreads(state, false);

            var inputModifier = GetInputModifier(state);

            var albumCoverInput = string.Empty;
            var mapArgs = string.Empty;
            var metadata = string.Empty;
            var vn = string.Empty;

            var hasArt = !string.IsNullOrWhiteSpace(state.AlbumCoverPath);
            hasArt = false;

            if (hasArt)
            {
                albumCoverInput = " -i \"" + state.AlbumCoverPath + "\"";
                mapArgs = " -map 0:a -map 1:v -c:v copy";
                metadata = " -metadata:s:v title=\"Album cover\" -metadata:s:v comment=\"Cover(Front)\"";
            }
            else
            {
                vn = " -vn";
            }

            var result = string.Format("{0} {1}{6}{7} -threads {2}{3} {4} -id3v2_version 3 -write_id3v1 1{8} -y \"{5}\"",
                inputModifier,
                GetInputArgument(state),
                threads,
                vn,
                string.Join(" ", audioTranscodeParams.ToArray()),
                state.OutputFilePath,
                albumCoverInput,
                mapArgs,
                metadata).Trim();

            return Task.FromResult(result);
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
