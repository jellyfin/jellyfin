using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public static class EncodingUtils
    {
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");
        
        public static string GetInputArgument(List<string> inputFiles, bool isRemote)
        {
            if (isRemote)
            {
                return GetHttpInputArgument(inputFiles);
            }

            return GetConcatInputArgument(inputFiles);
        }

        /// <summary>
        /// Gets the concat input argument.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <returns>System.String.</returns>
        private static string GetConcatInputArgument(List<string> inputFiles)
        {
            // Get all streams
            // If there's more than one we'll need to use the concat command
            if (inputFiles.Count > 1)
            {
                var files = string.Join("|", inputFiles);

                return string.Format("concat:\"{0}\"", files);
            }

            // Determine the input path for video files
            return GetFileInputArgument(inputFiles[0]);
        }

        /// <summary>
        /// Gets the file input argument.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        private static string GetFileInputArgument(string path)
        {
            return string.Format("file:\"{0}\"", path);
        }

        /// <summary>
        /// Gets the HTTP input argument.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <returns>System.String.</returns>
        private static string GetHttpInputArgument(IEnumerable<string> inputFiles)
        {
            var url = inputFiles.First();

            return string.Format("\"{0}\"", url);
        }

        public static string GetAudioInputModifier(InternalEncodingTask options)
        {
            return GetCommonInputModifier(options);
        }

        public static string GetInputModifier(InternalEncodingTask options)
        {
            var inputModifier = GetCommonInputModifier(options);

            //if (state.VideoRequest != null)
            //{
            //    inputModifier += " -fflags genpts";
            //}

            //if (!string.IsNullOrEmpty(state.InputVideoCodec))
            //{
            //    inputModifier += " -vcodec " + state.InputVideoCodec;
            //}

            //if (!string.IsNullOrEmpty(state.InputVideoSync))
            //{
            //    inputModifier += " -vsync " + state.InputVideoSync;
            //}

            return inputModifier;
        }

        private static string GetCommonInputModifier(InternalEncodingTask options)
        {
            var inputModifier = string.Empty;

            if (options.EnableDebugLogging)
            {
                inputModifier += "-loglevel debug";
            }

            var probeSize = GetProbeSizeArgument(options.InputVideoType.HasValue && options.InputVideoType.Value == VideoType.Dvd);
            inputModifier += " " + probeSize;
            inputModifier = inputModifier.Trim();

            if (!string.IsNullOrWhiteSpace(options.UserAgent))
            {
                inputModifier += " -user-agent \"" + options.UserAgent + "\"";
            }

            inputModifier += " " + GetFastSeekValue(options.Request);
            inputModifier = inputModifier.Trim();

            if (!string.IsNullOrEmpty(options.InputFormat))
            {
                inputModifier += " -f " + options.InputFormat;
            }

            if (!string.IsNullOrEmpty(options.InputAudioCodec))
            {
                inputModifier += " -acodec " + options.InputAudioCodec;
            }

            if (!string.IsNullOrEmpty(options.InputAudioSync))
            {
                inputModifier += " -async " + options.InputAudioSync;
            }

            if (options.ReadInputAtNativeFramerate)
            {
                inputModifier += " -re";
            }

            return inputModifier;
        }

        private static string GetFastSeekValue(EncodingOptions options)
        {
            var time = options.StartTimeTicks;

            if (time.HasValue)
            {
                var seconds = TimeSpan.FromTicks(time.Value).TotalSeconds;

                if (seconds > 0)
                {
                    return string.Format("-ss {0}", seconds.ToString(UsCulture));
                }
            }

            return string.Empty;
        }

        public static string GetProbeSizeArgument(bool isDvd)
        {
            return isDvd ? "-probesize 1G -analyzeduration 200M" : string.Empty;
        }

        public static int? GetAudioBitrateParam(InternalEncodingTask task)
        {
            if (task.Request.AudioBitRate.HasValue)
            {
                // Make sure we don't request a bitrate higher than the source
                var currentBitrate = task.AudioStream == null ? task.Request.AudioBitRate.Value : task.AudioStream.BitRate ?? task.Request.AudioBitRate.Value;

                return Math.Min(currentBitrate, task.Request.AudioBitRate.Value);
            }

            return null;
        }

        /// <summary>
        /// Gets the number of audio channels to specify on the command line
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="audioStream">The audio stream.</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        public static int? GetNumAudioChannelsParam(EncodingOptions request, MediaStream audioStream)
        {
            if (audioStream != null)
            {
                var codec = request.AudioCodec ?? string.Empty;

                if (audioStream.Channels > 2 && codec.IndexOf("wma", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    // wmav2 currently only supports two channel output
                    return 2;
                }
            }

            if (request.MaxAudioChannels.HasValue)
            {
                if (audioStream != null && audioStream.Channels.HasValue)
                {
                    return Math.Min(request.MaxAudioChannels.Value, audioStream.Channels.Value);
                }

                return request.MaxAudioChannels.Value;
            }

            return request.AudioChannels;
        }

        public static int GetNumberOfThreads(InternalEncodingTask state, bool isWebm)
        {
            // Use more when this is true. -re will keep cpu usage under control
            if (state.ReadInputAtNativeFramerate)
            {
                if (isWebm)
                {
                    return Math.Max(Environment.ProcessorCount - 1, 1);
                }

                return 0;
            }

            // Webm: http://www.webmproject.org/docs/encoder-parameters/
            // The decoder will usually automatically use an appropriate number of threads according to how many cores are available but it can only use multiple threads 
            // for the coefficient data if the encoder selected --token-parts > 0 at encode time.

            switch (state.QualitySetting)
            {
                case EncodingQuality.HighSpeed:
                    return 2;
                case EncodingQuality.HighQuality:
                    return 2;
                case EncodingQuality.MaxQuality:
                    return isWebm ? 2 : 0;
                default:
                    throw new Exception("Unrecognized MediaEncodingQuality value.");
            }
        }
    }
}
