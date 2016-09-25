using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Api.Playback.Hls
{
    /// <summary>
    /// Class BaseHlsService
    /// </summary>
    public abstract class BaseHlsService : BaseStreamingService
    {
        protected BaseHlsService(IServerConfigurationManager serverConfig, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IFileSystem fileSystem, IDlnaManager dlnaManager, ISubtitleEncoder subtitleEncoder, IDeviceManager deviceManager, IMediaSourceManager mediaSourceManager, IZipClient zipClient, IJsonSerializer jsonSerializer)
            : base(serverConfig, userManager, libraryManager, isoManager, mediaEncoder, fileSystem, dlnaManager, subtitleEncoder, deviceManager, mediaSourceManager, zipClient, jsonSerializer)
        {
        }

        /// <summary>
        /// Gets the audio arguments.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected abstract string GetAudioArguments(StreamState state);
        /// <summary>
        /// Gets the video arguments.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected abstract string GetVideoArguments(StreamState state);

        /// <summary>
        /// Gets the segment file extension.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected abstract string GetSegmentFileExtension(StreamState state);

        /// <summary>
        /// Gets the type of the transcoding job.
        /// </summary>
        /// <value>The type of the transcoding job.</value>
        protected override TranscodingJobType TranscodingJobType
        {
            get { return TranscodingJobType.Hls; }
        }

        /// <summary>
        /// Processes the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="isLive">if set to <c>true</c> [is live].</param>
        /// <returns>System.Object.</returns>
        protected async Task<object> ProcessRequest(StreamRequest request, bool isLive)
        {
            return await ProcessRequestAsync(request, isLive).ConfigureAwait(false);
        }

        /// <summary>
        /// Processes the request async.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="isLive">if set to <c>true</c> [is live].</param>
        /// <returns>Task{System.Object}.</returns>
        /// <exception cref="ArgumentException">A video bitrate is required
        /// or
        /// An audio bitrate is required</exception>
        private async Task<object> ProcessRequestAsync(StreamRequest request, bool isLive)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var state = await GetState(request, cancellationTokenSource.Token).ConfigureAwait(false);

            TranscodingJob job = null;
            var playlist = state.OutputFilePath;

            if (!FileSystem.FileExists(playlist))
            {
                var transcodingLock = ApiEntryPoint.Instance.GetTranscodingLock(playlist);
                await transcodingLock.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
                try
                {
                    if (!FileSystem.FileExists(playlist))
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

                        var waitForSegments = state.SegmentLength >= 10 ? 2 : (state.SegmentLength > 3 || !isLive ? 3 : 3);
                        await WaitForMinimumSegmentCount(playlist, waitForSegments, cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }
                finally
                {
                    transcodingLock.Release();
                }
            }

            if (isLive)
            {
                job = job ?? ApiEntryPoint.Instance.OnTranscodeBeginRequest(playlist, TranscodingJobType);

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

            job = job ?? ApiEntryPoint.Instance.OnTranscodeBeginRequest(playlist, TranscodingJobType);

            if (job != null)
            {
                ApiEntryPoint.Instance.OnTranscodeEndRequest(job);
            }

            return ResultFactory.GetResult(playlistText, MimeTypes.GetMimeType("playlist.m3u8"), new Dictionary<string, string>());
        }

        private string GetLivePlaylistText(string path, int segmentLength)
        {
            using (var stream = FileSystem.GetFileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    var text = reader.ReadToEnd();

                    text = text.Replace("#EXTM3U", "#EXTM3U\n#EXT-X-PLAYLIST-TYPE:EVENT");

                    var newDuration = "#EXT-X-TARGETDURATION:" + segmentLength.ToString(UsCulture);

                    text = text.Replace("#EXT-X-TARGETDURATION:" + (segmentLength + 1).ToString(UsCulture), newDuration, StringComparison.OrdinalIgnoreCase);

                    return text;
                }
            }
        }

        private string GetMasterPlaylistFileText(string firstPlaylist, int bitrate, int baselineStreamBitrate)
        {
            var builder = new StringBuilder();

            builder.AppendLine("#EXTM3U");

            // Pad a little to satisfy the apple hls validator
            var paddedBitrate = Convert.ToInt32(bitrate * 1.15);

            // Main stream
            builder.AppendLine("#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH=" + paddedBitrate.ToString(UsCulture));
            var playlistUrl = "hls/" + Path.GetFileName(firstPlaylist).Replace(".m3u8", "/stream.m3u8");
            builder.AppendLine(playlistUrl);

            return builder.ToString();
        }

        protected virtual async Task WaitForMinimumSegmentCount(string playlist, int segmentCount, CancellationToken cancellationToken)
        {
            Logger.Debug("Waiting for {0} segments in {1}", segmentCount, playlist);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Need to use FileShare.ReadWrite because we're reading the file at the same time it's being written
                    using (var fileStream = GetPlaylistFileStream(playlist))
                    {
                        using (var reader = new StreamReader(fileStream))
                        {
                            var count = 0;

                            while (!reader.EndOfStream)
                            {
                                var line = await reader.ReadLineAsync().ConfigureAwait(false);

                                if (line.IndexOf("#EXTINF:", StringComparison.OrdinalIgnoreCase) != -1)
                                {
                                    count++;
                                    if (count >= segmentCount)
                                    {
                                        Logger.Debug("Finished waiting for {0} segments in {1}", segmentCount, playlist);
                                        return;
                                    }
                                }
                            }
                            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                        }
                    }
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
            var tmpPath = path + ".tmp";

            try
            {
                return FileSystem.GetFileStream(tmpPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, true);
            }
            catch (IOException)
            {
                return FileSystem.GetFileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, true);
            }
        }

        protected override string GetCommandLineArguments(string outputPath, StreamState state, bool isEncoding)
        {
            var itsOffsetMs = 0;

            var itsOffset = itsOffsetMs == 0 ? string.Empty : string.Format("-itsoffset {0} ", TimeSpan.FromMilliseconds(itsOffsetMs).TotalSeconds.ToString(UsCulture));

            var threads = GetNumberOfThreads(state, false);

            var inputModifier = GetInputModifier(state);

            // If isEncoding is true we're actually starting ffmpeg
            var startNumberParam = isEncoding ? GetStartNumber(state).ToString(UsCulture) : "0";

            var baseUrlParam = string.Empty;

            if (state.Request is GetLiveHlsStream)
            {
                baseUrlParam = string.Format(" -hls_base_url \"{0}/\"",
                    "hls/" + Path.GetFileNameWithoutExtension(outputPath));
            }

            var args = string.Format("{0} {1} {2} -map_metadata -1 -threads {3} {4} {5} -sc_threshold 0 {6} -hls_time {7} -start_number {8} -hls_list_size {9}{10} -y \"{11}\"",
                itsOffset,
                inputModifier,
                GetInputArgument(state),
                threads,
                GetMapArgs(state),
                GetVideoArguments(state),
                GetAudioArguments(state),
                state.SegmentLength.ToString(UsCulture),
                startNumberParam,
                state.HlsListSize.ToString(UsCulture),
                baseUrlParam,
                outputPath
                ).Trim();

            return args;
        }

        protected virtual int GetStartNumber(StreamState state)
        {
            return 0;
        }

        protected bool IsLiveStream(StreamState state)
        {
            var isLiveStream = (state.RunTimeTicks ?? 0) == 0;

            return isLiveStream;
        }
    }
}