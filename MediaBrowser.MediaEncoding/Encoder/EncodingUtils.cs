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
        private static string GetConcatInputArgument(IReadOnlyList<string> inputFiles)
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
    }
}
