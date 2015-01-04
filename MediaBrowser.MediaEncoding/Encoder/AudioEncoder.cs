using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class AudioEncoder : BaseEncoder
    {
        public AudioEncoder(MediaEncoder mediaEncoder, ILogger logger, IServerConfigurationManager configurationManager, IFileSystem fileSystem, ILiveTvManager liveTvManager, IIsoManager isoManager, ILibraryManager libraryManager, IChannelManager channelManager, ISessionManager sessionManager, ISubtitleEncoder subtitleEncoder) : base(mediaEncoder, logger, configurationManager, fileSystem, liveTvManager, isoManager, libraryManager, channelManager, sessionManager, subtitleEncoder)
        {
        }

        protected override string GetCommandLineArguments(EncodingJob job)
        {
            var audioTranscodeParams = new List<string>();

            var bitrate = job.OutputAudioBitrate;

            if (bitrate.HasValue)
            {
                audioTranscodeParams.Add("-ab " + bitrate.Value.ToString(UsCulture));
            }

            if (job.OutputAudioChannels.HasValue)
            {
                audioTranscodeParams.Add("-ac " + job.OutputAudioChannels.Value.ToString(UsCulture));
            }

            if (job.OutputAudioSampleRate.HasValue)
            {
                audioTranscodeParams.Add("-ar " + job.OutputAudioSampleRate.Value.ToString(UsCulture));
            }

            var threads = GetNumberOfThreads(job, false);

            var inputModifier = GetInputModifier(job);

            return string.Format("{0} {1} -threads {2}{3} {4} -id3v2_version 3 -write_id3v1 1 -y \"{5}\"",
                inputModifier,
                GetInputArgument(job),
                threads,
                " -vn",
                string.Join(" ", audioTranscodeParams.ToArray()),
                job.OutputFilePath).Trim();
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
    }
}
