using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class AudioEncoder
    {
        private readonly string _ffmpegPath;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IApplicationPaths _appPaths;
        private readonly IIsoManager _isoManager;
        private readonly ILiveTvManager _liveTvManager;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public AudioEncoder(string ffmpegPath, ILogger logger, IFileSystem fileSystem, IApplicationPaths appPaths, IIsoManager isoManager, ILiveTvManager liveTvManager)
        {
            _ffmpegPath = ffmpegPath;
            _logger = logger;
            _fileSystem = fileSystem;
            _appPaths = appPaths;
            _isoManager = isoManager;
            _liveTvManager = liveTvManager;
        }

        public Task BeginEncoding(InternalEncodingTask task)
        {
            return new FFMpegProcess(_ffmpegPath, _logger, _fileSystem, _appPaths, _isoManager, _liveTvManager).Start(task, GetArguments);
        }

        private string GetArguments(InternalEncodingTask task, string mountedPath)
        {
            var options = task.Request;

            return string.Format("{0} -i {1} {2} -id3v2_version 3 -write_id3v1 1 \"{3}\"",
                GetInputModifier(task),
                GetInputArgument(task),
                GetOutputModifier(task),
                options.OutputPath).Trim();
        }

        private string GetInputModifier(InternalEncodingTask task)
        {
            return EncodingUtils.GetInputModifier(task);
        }

        private string GetInputArgument(InternalEncodingTask task)
        {
            return EncodingUtils.GetInputArgument(new List<string> { task.MediaPath }, task.IsInputRemote);
        }

        private string GetOutputModifier(InternalEncodingTask task)
        {
            var options = task.Request;

            var audioTranscodeParams = new List<string>
            {
                "-threads " + EncodingUtils.GetNumberOfThreads(task, false).ToString(_usCulture),
                "-vn"
            };

            var bitrate = EncodingUtils.GetAudioBitrateParam(task);

            if (bitrate.HasValue)
            {
                audioTranscodeParams.Add("-ab " + bitrate.Value.ToString(_usCulture));
            }

            var channels = EncodingUtils.GetNumAudioChannelsParam(options, task.AudioStream);

            if (channels.HasValue)
            {
                audioTranscodeParams.Add("-ac " + channels.Value);
            }

            if (options.AudioSampleRate.HasValue)
            {
                audioTranscodeParams.Add("-ar " + options.AudioSampleRate.Value);
            }

            return string.Join(" ", audioTranscodeParams.ToArray());
        }
    }
}
