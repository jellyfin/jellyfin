using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.Playback.Hls
{
    /// <summary>
    /// Class BaseHlsService
    /// </summary>
    public abstract class BaseHlsService : BaseStreamingService
    {
        public BaseHlsService(
            ILogger<BaseHlsService> logger,
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

        /// <summary>
        /// Gets the audio arguments.
        /// </summary>
        protected abstract string GetAudioArguments(StreamState state, EncodingOptions encodingOptions);

        /// <summary>
        /// Gets the video arguments.
        /// </summary>
        protected abstract string GetVideoArguments(StreamState state, EncodingOptions encodingOptions);

        /// <summary>
        /// Gets the segment file extension.
        /// </summary>
        protected string GetSegmentFileExtension(StreamRequest request)
        {
            var segmentContainer = request.SegmentContainer;
            if (!string.IsNullOrWhiteSpace(segmentContainer))
            {
                return "." + segmentContainer;
            }

            return ".ts";
        }

        /// <summary>
        /// Gets the type of the transcoding job.
        /// </summary>
        /// <value>The type of the transcoding job.</value>
        protected override TranscodingJobType TranscodingJobType => TranscodingJobType.Hls;

        /// <summary>
        /// Processes the request async.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="isLive">if set to <c>true</c> [is live].</param>
        /// <returns>Task{System.Object}.</returns>
        /// <exception cref="ArgumentException">A video bitrate is required
        /// or
        /// An audio bitrate is required</exception>
        protected async Task<object> ProcessRequestAsync(StreamRequest request, bool isLive)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var state = await GetState(request, cancellationTokenSource.Token).ConfigureAwait(false);

            TranscodingJob job = null;
            var playlist = state.OutputFilePath;

            if (!File.Exists(playlist))
            {
                var transcodingLock = ApiEntryPoint.Instance.GetTranscodingLock(playlist);
                await transcodingLock.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
                try
                {
                    if (!File.Exists(playlist))
                    {
                        // If the playlist doesn't already exist, startup ffmpeg
                        try
                        {
                            job = await StartFfMpeg(state, playlist, cancellationTokenSource).ConfigureAwait(false);
                            job.IsLiveOutput = isLive;
                        }
                        catch
                        {
                            state.Dispose();
                            throw;
                        }

                        var minSegments = state.MinSegments;
                        if (minSegments > 0)
                        {
                            await WaitForMinimumSegmentCount(playlist, minSegments, cancellationTokenSource.Token).ConfigureAwait(false);
                        }
                    }
                }
                finally
                {
                    transcodingLock.Release();
                }
            }

            if (isLive)
            {
                job ??= ApiEntryPoint.Instance.OnTranscodeBeginRequest(playlist, TranscodingJobType);

                if (job != null)
                {
                    ApiEntryPoint.Instance.OnTranscodeEndRequest(job);
                }
                return ResultFactory.GetResult(GetLivePlaylistText(playlist, state.SegmentLength), MimeTypes.GetMimeType("playlist.m3u8"), new Dictionary<string, string>());
            }

            var audioBitrate = state.OutputAudioBitrate ?? 0;
            var videoBitrate = state.OutputVideoBitrate ?? 0;

            var baselineStreamBitrate = 64000;

            var playlistText = GetMasterPlaylistFileText(playlist, videoBitrate + audioBitrate, baselineStreamBitrate);

            job ??= ApiEntryPoint.Instance.OnTranscodeBeginRequest(playlist, TranscodingJobType);

            if (job != null)
            {
                ApiEntryPoint.Instance.OnTranscodeEndRequest(job);
            }

            return ResultFactory.GetResult(playlistText, MimeTypes.GetMimeType("playlist.m3u8"), new Dictionary<string, string>());
        }

        private string GetLivePlaylistText(string path, int segmentLength)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);

            var text = reader.ReadToEnd();

            text = text.Replace("#EXTM3U", "#EXTM3U\n#EXT-X-PLAYLIST-TYPE:EVENT");

            var newDuration = "#EXT-X-TARGETDURATION:" + segmentLength.ToString(CultureInfo.InvariantCulture);

            text = text.Replace("#EXT-X-TARGETDURATION:" + (segmentLength - 1).ToString(CultureInfo.InvariantCulture), newDuration, StringComparison.OrdinalIgnoreCase);
            //text = text.Replace("#EXT-X-TARGETDURATION:" + (segmentLength + 1).ToString(CultureInfo.InvariantCulture), newDuration, StringComparison.OrdinalIgnoreCase);

            return text;
        }

        private string GetMasterPlaylistFileText(string firstPlaylist, int bitrate, int baselineStreamBitrate)
        {
            var builder = new StringBuilder();

            builder.AppendLine("#EXTM3U");

            // Pad a little to satisfy the apple hls validator
            var paddedBitrate = Convert.ToInt32(bitrate * 1.15);

            // Main stream
            builder.AppendLine("#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH=" + paddedBitrate.ToString(CultureInfo.InvariantCulture));
            var playlistUrl = "hls/" + Path.GetFileName(firstPlaylist).Replace(".m3u8", "/stream.m3u8");
            builder.AppendLine(playlistUrl);

            return builder.ToString();
        }

        protected virtual async Task WaitForMinimumSegmentCount(string playlist, int segmentCount, CancellationToken cancellationToken)
        {
            Logger.LogDebug("Waiting for {0} segments in {1}", segmentCount, playlist);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Need to use FileShare.ReadWrite because we're reading the file at the same time it's being written
                    var fileStream = GetPlaylistFileStream(playlist);
                    await using (fileStream.ConfigureAwait(false))
                    {
                        using var reader = new StreamReader(fileStream);
                        var count = 0;

                        while (!reader.EndOfStream)
                        {
                            var line = await reader.ReadLineAsync().ConfigureAwait(false);

                            if (line.IndexOf("#EXTINF:", StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                count++;
                                if (count >= segmentCount)
                                {
                                    Logger.LogDebug("Finished waiting for {0} segments in {1}", segmentCount, playlist);
                                    return;
                                }
                            }
                        }
                    }

                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
                catch (IOException)
                {
                    // May get an error if the file is locked
                }

                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            }
        }

        protected Stream GetPlaylistFileStream(string path)
        {
            return new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                IODefaults.FileStreamBufferSize,
                FileOptions.SequentialScan);
        }

        protected override string GetCommandLineArguments(string outputPath, EncodingOptions encodingOptions, StreamState state, bool isEncoding)
        {
            var itsOffsetMs = 0;

            var itsOffset = itsOffsetMs == 0 ? string.Empty : string.Format("-itsoffset {0} ", TimeSpan.FromMilliseconds(itsOffsetMs).TotalSeconds.ToString(CultureInfo.InvariantCulture));

            var videoCodec = EncodingHelper.GetVideoEncoder(state, encodingOptions);

            var threads = EncodingHelper.GetNumberOfThreads(state, encodingOptions, videoCodec);

            var inputModifier = EncodingHelper.GetInputModifier(state, encodingOptions);

            // If isEncoding is true we're actually starting ffmpeg
            var startNumberParam = isEncoding ? GetStartNumber(state).ToString(CultureInfo.InvariantCulture) : "0";

            var baseUrlParam = string.Empty;

            if (state.Request is GetLiveHlsStream)
            {
                baseUrlParam = string.Format(" -hls_base_url \"{0}/\"",
                    "hls/" + Path.GetFileNameWithoutExtension(outputPath));
            }

            var useGenericSegmenter = true;
            if (useGenericSegmenter)
            {
                var outputTsArg = Path.Combine(Path.GetDirectoryName(outputPath), Path.GetFileNameWithoutExtension(outputPath)) + "%d" + GetSegmentFileExtension(state.Request);

                var timeDeltaParam = string.Empty;

                var segmentFormat = GetSegmentFileExtension(state.Request).TrimStart('.');
                if (string.Equals(segmentFormat, "ts", StringComparison.OrdinalIgnoreCase))
                {
                    segmentFormat = "mpegts";
                }

                baseUrlParam = string.Format("\"{0}/\"", "hls/" + Path.GetFileNameWithoutExtension(outputPath));

                return string.Format("{0} {1} -map_metadata -1 -map_chapters -1 -threads {2} {3} {4} {5} -f segment -max_delay 5000000 -avoid_negative_ts disabled -start_at_zero -segment_time {6} {10} -individual_header_trailer 0 -segment_format {11} -segment_list_entry_prefix {12} -segment_list_type m3u8 -segment_start_number {7} -segment_list \"{8}\" -y \"{9}\"",
                    inputModifier,
                    EncodingHelper.GetInputArgument(state, encodingOptions),
                    threads,
                    EncodingHelper.GetMapArgs(state),
                    GetVideoArguments(state, encodingOptions),
                    GetAudioArguments(state, encodingOptions),
                    state.SegmentLength.ToString(CultureInfo.InvariantCulture),
                    startNumberParam,
                    outputPath,
                    outputTsArg,
                    timeDeltaParam,
                    segmentFormat,
                    baseUrlParam
                ).Trim();
            }

            // add when stream copying?
            // -avoid_negative_ts make_zero -fflags +genpts

            var args = string.Format("{0} {1} {2} -map_metadata -1 -map_chapters -1 -threads {3} {4} {5} -max_delay 5000000 -avoid_negative_ts disabled -start_at_zero {6} -hls_time {7} -individual_header_trailer 0 -start_number {8} -hls_list_size {9}{10} -y \"{11}\"",
                itsOffset,
                inputModifier,
                EncodingHelper.GetInputArgument(state, encodingOptions),
                threads,
                EncodingHelper.GetMapArgs(state),
                GetVideoArguments(state, encodingOptions),
                GetAudioArguments(state, encodingOptions),
                state.SegmentLength.ToString(CultureInfo.InvariantCulture),
                startNumberParam,
                state.HlsListSize.ToString(CultureInfo.InvariantCulture),
                baseUrlParam,
                outputPath
                ).Trim();

            return args;
        }

        protected override string GetDefaultEncoderPreset()
        {
            return "veryfast";
        }

        protected virtual int GetStartNumber(StreamState state)
        {
            return 0;
        }
    }
}
