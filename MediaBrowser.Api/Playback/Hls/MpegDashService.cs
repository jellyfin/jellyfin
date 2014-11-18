using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.IO;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Playback.Hls
{
    /// <summary>
    /// Options is needed for chromecast. Threw Head in there since it's related
    /// </summary>
    [Route("/Videos/{Id}/master.mpd", "GET", Summary = "Gets a video stream using Mpeg dash.")]
    [Route("/Videos/{Id}/master.mpd", "HEAD", Summary = "Gets a video stream using Mpeg dash.")]
    public class GetMasterManifest : VideoStreamRequest
    {
        public bool EnableAdaptiveBitrateStreaming { get; set; }

        public GetMasterManifest()
        {
            EnableAdaptiveBitrateStreaming = true;
        }
    }

    [Route("/Videos/{Id}/dash/{SegmentId}.ts", "GET")]
    [Route("/Videos/{Id}/dash/{SegmentId}.mp4", "GET")]
    public class GetDashSegment : VideoStreamRequest
    {
        /// <summary>
        /// Gets or sets the segment id.
        /// </summary>
        /// <value>The segment id.</value>
        public string SegmentId { get; set; }
    }

    public class MpegDashService : BaseHlsService
    {
        protected INetworkManager NetworkManager { get; private set; }

        public MpegDashService(IServerConfigurationManager serverConfig, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IFileSystem fileSystem, ILiveTvManager liveTvManager, IDlnaManager dlnaManager, IChannelManager channelManager, ISubtitleEncoder subtitleEncoder, INetworkManager networkManager)
            : base(serverConfig, userManager, libraryManager, isoManager, mediaEncoder, fileSystem, liveTvManager, dlnaManager, channelManager, subtitleEncoder)
        {
            NetworkManager = networkManager;
        }

        public object Get(GetMasterManifest request)
        {
            var result = GetAsync(request, "GET").Result;

            return result;
        }

        public object Head(GetMasterManifest request)
        {
            var result = GetAsync(request, "HEAD").Result;

            return result;
        }

        private async Task<object> GetAsync(GetMasterManifest request, string method)
        {
            if (string.Equals(request.AudioCodec, "copy", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Audio codec copy is not allowed here.");
            }

            if (string.Equals(request.VideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Video codec copy is not allowed here.");
            }

            if (string.IsNullOrEmpty(request.MediaSourceId))
            {
                throw new ArgumentException("MediaSourceId is required");
            }

            var state = await GetState(request, CancellationToken.None).ConfigureAwait(false);

            var playlistText = string.Empty;

            if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                playlistText = GetManifestText(state);
            }

            return ResultFactory.GetResult(playlistText, Common.Net.MimeTypes.GetMimeType("playlist.mpd"), new Dictionary<string, string>());
        }

        private string GetManifestText(StreamState state)
        {
            var builder = new StringBuilder();

            var time = TimeSpan.FromTicks(state.RunTimeTicks.Value);

            var duration = "PT" + time.Hours.ToString("00", UsCulture) + "H" + time.Minutes.ToString("00", UsCulture) + "M" + time.Seconds.ToString("00", UsCulture) + ".00S";

            builder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");

            var profile = string.Equals(GetSegmentFileExtension(state), ".ts", StringComparison.OrdinalIgnoreCase)
                ? "urn:mpeg:dash:profile:mp2t-simple:2011"
                : "urn:mpeg:dash:profile:mp2t-simple:2011";

            builder.AppendFormat(
                "<MPD xmlns=\"urn:mpeg:dash:schema:mpd:2011\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"urn:mpeg:dash:schema:mpd:2011 http://standards.iso.org/ittf/PubliclyAvailableStandards/MPEG-DASH_schema_files/DASH-MPD.xsd\" minBufferTime=\"PT2.00S\" mediaPresentationDuration=\"{0}\" maxSegmentDuration=\"PT{1}S\" type=\"static\" profiles=\""+profile+"\">",
                duration,
                state.SegmentLength.ToString(CultureInfo.InvariantCulture));

            builder.Append("<ProgramInformation moreInformationURL=\"http://gpac.sourceforge.net\">");
            builder.Append("</ProgramInformation>");

            builder.AppendFormat("<Period start=\"PT0S\" duration=\"{0}\">", duration);
            builder.Append("<AdaptationSet segmentAlignment=\"true\">");

            builder.Append("<ContentComponent id=\"1\" contentType=\"video\"/>");

            var lang = state.AudioStream != null ? state.AudioStream.Language : null;
            if (string.IsNullOrWhiteSpace(lang)) lang = "und";

            builder.AppendFormat("<ContentComponent id=\"2\" contentType=\"audio\" lang=\"{0}\"/>", lang);

            builder.Append(GetRepresentationOpenElement(state, lang));

            AppendSegmentList(state, builder);

            builder.Append("</Representation>");
            builder.Append("</AdaptationSet>");
            builder.Append("</Period>");

            builder.Append("</MPD>");

            return builder.ToString();
        }

        private string GetRepresentationOpenElement(StreamState state, string language)
        {
            var codecs = GetVideoCodecDescriptor(state) + "," + GetAudioCodecDescriptor(state);

            var mime = string.Equals(GetSegmentFileExtension(state), ".ts", StringComparison.OrdinalIgnoreCase)
                ? "video/mp2t"
                : "video/mp4";

            var xml = "<Representation id=\"1\" mimeType=\"" + mime + "\" startWithSAP=\"1\" codecs=\"" + codecs + "\"";

            if (state.OutputWidth.HasValue)
            {
                xml += " width=\"" + state.OutputWidth.Value.ToString(UsCulture) + "\"";
            }
            if (state.OutputHeight.HasValue)
            {
                xml += " height=\"" + state.OutputHeight.Value.ToString(UsCulture) + "\"";
            }
            if (state.OutputAudioSampleRate.HasValue)
            {
                xml += " sampleRate=\"" + state.OutputAudioSampleRate.Value.ToString(UsCulture) + "\"";
            }

            if (state.TotalOutputBitrate.HasValue)
            {
                xml += " bandwidth=\"" + state.TotalOutputBitrate.Value.ToString(UsCulture) + "\"";
            }

            xml += ">";

            return xml;
        }

        private string GetVideoCodecDescriptor(StreamState state)
        {
            // https://developer.apple.com/library/ios/documentation/networkinginternet/conceptual/streamingmediaguide/FrequentlyAskedQuestions/FrequentlyAskedQuestions.html
            // http://www.chipwreck.de/blog/2010/02/25/html-5-video-tag-and-attributes/

            var level = state.TargetVideoLevel ?? 0;
            var profile = state.TargetVideoProfile ?? string.Empty;

            if (profile.IndexOf("high", StringComparison.OrdinalIgnoreCase) != -1)
            {
                if (level >= 4.1)
                {
                    return "avc1.640028";
                }

                if (level >= 4)
                {
                    return "avc1.640028";
                }

                return "avc1.64001f";
            }

            if (profile.IndexOf("main", StringComparison.OrdinalIgnoreCase) != -1)
            {
                if (level >= 4)
                {
                    return "avc1.4d0028";
                }

                if (level >= 3.1)
                {
                    return "avc1.4d001f";
                }

                return "avc1.4d001e";
            }

            if (level >= 3.1)
            {
                return "avc1.42001f";
            }

            return "avc1.42E01E";
        }

        private string GetAudioCodecDescriptor(StreamState state)
        {
            // https://developer.apple.com/library/ios/documentation/networkinginternet/conceptual/streamingmediaguide/FrequentlyAskedQuestions/FrequentlyAskedQuestions.html

            if (string.Equals(state.OutputAudioCodec, "mp3", StringComparison.OrdinalIgnoreCase))
            {
                return "mp4a.40.34";
            }

            // AAC 5ch
            if (state.OutputAudioChannels.HasValue && state.OutputAudioChannels.Value >= 5)
            {
                return "mp4a.40.5";
            }

            // AAC 2ch
            return "mp4a.40.2";
        }

        public object Get(GetDashSegment request)
        {
            return GetDynamicSegment(request, request.SegmentId).Result;
        }

        private void AppendSegmentList(StreamState state, StringBuilder builder)
        {
            var extension = GetSegmentFileExtension(state);

            var seconds = TimeSpan.FromTicks(state.RunTimeTicks ?? 0).TotalSeconds;

            var queryStringIndex = Request.RawUrl.IndexOf('?');
            var queryString = queryStringIndex == -1 ? string.Empty : Request.RawUrl.Substring(queryStringIndex);

            var index = 0;
            builder.Append("<SegmentList timescale=\"1000\" duration=\"10000\">");


            while (seconds > 0)
            {
                var segmentUrl = string.Format("dash/{0}{1}{2}",
                    index.ToString(UsCulture),
                    extension,
                    SecurityElement.Escape(queryString));

                if (index == 0)
                {
                    builder.AppendFormat("<Initialization sourceURL=\"{0}\"/>", segmentUrl);
                }
                else
                {
                    builder.AppendFormat("<SegmentURL media=\"{0}\"/>", segmentUrl);
                }

                seconds -= state.SegmentLength;
                index++;
            }
            builder.Append("</SegmentList>");
        }

        private async Task<object> GetDynamicSegment(VideoStreamRequest request, string segmentId)
        {
            if ((request.StartTimeTicks ?? 0) > 0)
            {
                throw new ArgumentException("StartTimeTicks is not allowed.");
            }

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var index = int.Parse(segmentId, NumberStyles.Integer, UsCulture);

            var state = await GetState(request, cancellationToken).ConfigureAwait(false);

            var playlistPath = Path.ChangeExtension(state.OutputFilePath, ".m3u8");

            var segmentExtension = GetSegmentFileExtension(state);

            var segmentPath = GetSegmentPath(playlistPath, segmentExtension, index);
            var segmentLength = state.SegmentLength;

            TranscodingJob job = null;

            if (File.Exists(segmentPath))
            {
                return await GetSegmentResult(playlistPath, segmentPath, index, segmentLength, job, cancellationToken).ConfigureAwait(false);
            }

            await ApiEntryPoint.Instance.TranscodingStartLock.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            try
            {
                if (File.Exists(segmentPath))
                {
                    return await GetSegmentResult(playlistPath, segmentPath, index, segmentLength, job, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var currentTranscodingIndex = GetCurrentTranscodingIndex(playlistPath, segmentExtension);

                    if (currentTranscodingIndex == null || index < currentTranscodingIndex.Value || (index - currentTranscodingIndex.Value) > 4)
                    {
                        // If the playlist doesn't already exist, startup ffmpeg
                        try
                        {
                            ApiEntryPoint.Instance.KillTranscodingJobs(j => j.Type == TranscodingJobType && string.Equals(j.DeviceId, request.DeviceId, StringComparison.OrdinalIgnoreCase), p => !string.Equals(p, playlistPath, StringComparison.OrdinalIgnoreCase));

                            if (currentTranscodingIndex.HasValue)
                            {
                                DeleteLastFile(playlistPath, segmentExtension, 0);
                            }

                            var startSeconds = index * state.SegmentLength;
                            request.StartTimeTicks = TimeSpan.FromSeconds(startSeconds).Ticks;

                            job = await StartFfMpeg(state, playlistPath, cancellationTokenSource, Path.GetDirectoryName(playlistPath)).ConfigureAwait(false);
                        }
                        catch
                        {
                            state.Dispose();
                            throw;
                        }

                        await WaitForMinimumSegmentCount(playlistPath, 2, cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                ApiEntryPoint.Instance.TranscodingStartLock.Release();
            }

            Logger.Info("waiting for {0}", segmentPath);
            while (!File.Exists(segmentPath))
            {
                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            }

            Logger.Info("returning {0}", segmentPath);
            job = job ?? ApiEntryPoint.Instance.GetTranscodingJob(playlistPath, TranscodingJobType);
            return await GetSegmentResult(playlistPath, segmentPath, index, segmentLength, job, cancellationToken).ConfigureAwait(false);
        }

        private async Task<object> GetSegmentResult(string playlistPath,
            string segmentPath,
            int segmentIndex,
            int segmentLength,
            TranscodingJob transcodingJob,
            CancellationToken cancellationToken)
        {
            // If all transcoding has completed, just return immediately
            if (!IsTranscoding(playlistPath))
            {
                return GetSegmentResult(segmentPath, segmentIndex, segmentLength, transcodingJob);
            }

            var segmentFilename = Path.GetFileName(segmentPath);

            using (var fileStream = FileSystem.GetFileStream(playlistPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, true))
            {
                using (var reader = new StreamReader(fileStream))
                {
                    var text = await reader.ReadToEndAsync().ConfigureAwait(false);

                    // If it appears in the playlist, it's done
                    if (text.IndexOf(segmentFilename, StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        return GetSegmentResult(segmentPath, segmentIndex, segmentLength, transcodingJob);
                    }
                }
            }

            // if a different file is encoding, it's done
            //var currentTranscodingIndex = GetCurrentTranscodingIndex(playlistPath);
            //if (currentTranscodingIndex > segmentIndex)
            //{
            //return GetSegmentResult(segmentPath, segmentIndex);
            //}

            // Wait for the file to stop being written to, then stream it
            var length = new FileInfo(segmentPath).Length;
            var eofCount = 0;

            while (eofCount < 10)
            {
                var info = new FileInfo(segmentPath);

                if (!info.Exists)
                {
                    break;
                }

                var newLength = info.Length;

                if (newLength == length)
                {
                    eofCount++;
                }
                else
                {
                    eofCount = 0;
                }

                length = newLength;
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            return GetSegmentResult(segmentPath, segmentIndex, segmentLength, transcodingJob);
        }

        private object GetSegmentResult(string segmentPath, int index, int segmentLength, TranscodingJob transcodingJob)
        {
            var segmentEndingSeconds = (1 + index) * segmentLength;
            var segmentEndingPositionTicks = TimeSpan.FromSeconds(segmentEndingSeconds).Ticks;

            return ResultFactory.GetStaticFileResult(Request, new StaticFileResultOptions
            {
                Path = segmentPath,
                FileShare = FileShare.ReadWrite,
                OnComplete = () =>
                {
                    if (transcodingJob != null)
                    {
                        transcodingJob.DownloadPositionTicks = Math.Max(transcodingJob.DownloadPositionTicks ?? segmentEndingPositionTicks, segmentEndingPositionTicks);
                    }

                }
            });
        }

        private bool IsTranscoding(string playlistPath)
        {
            var job = ApiEntryPoint.Instance.GetTranscodingJob(playlistPath, TranscodingJobType);

            return job != null && !job.HasExited;
        }
        
        public int? GetCurrentTranscodingIndex(string playlist, string segmentExtension)
        {
            var file = GetLastTranscodingFile(playlist, segmentExtension, FileSystem);

            if (file == null)
            {
                return null;
            }

            var playlistFilename = Path.GetFileNameWithoutExtension(playlist);

            var indexString = Path.GetFileNameWithoutExtension(file.Name).Substring(playlistFilename.Length);

            return int.Parse(indexString, NumberStyles.Integer, UsCulture);
        }

        private void DeleteLastFile(string path, string segmentExtension, int retryCount)
        {
            if (retryCount >= 5)
            {
                return;
            }

            var file = GetLastTranscodingFile(path, segmentExtension, FileSystem);

            if (file != null)
            {
                try
                {
                    File.Delete(file.FullName);
                }
                catch (IOException ex)
                {
                    Logger.ErrorException("Error deleting partial stream file(s) {0}", ex, file.FullName);

                    Thread.Sleep(100);
                    DeleteLastFile(path, segmentExtension, retryCount + 1);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error deleting partial stream file(s) {0}", ex, file.FullName);
                }
            }
        }

        private static FileInfo GetLastTranscodingFile(string playlist, string segmentExtension, IFileSystem fileSystem)
        {
            var folder = Path.GetDirectoryName(playlist);

            try
            {
                return new DirectoryInfo(folder)
                    .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                    .Where(i => string.Equals(i.Extension, segmentExtension, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(fileSystem.GetLastWriteTimeUtc)
                    .FirstOrDefault();
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
        }

        protected override int GetStartNumber(StreamState state)
        {
            return GetStartNumber(state.VideoRequest);
        }

        private int GetStartNumber(VideoStreamRequest request)
        {
            var segmentId = "0";

            var segmentRequest = request as GetDynamicHlsVideoSegment;
            if (segmentRequest != null)
            {
                segmentId = segmentRequest.SegmentId;
            }

            return int.Parse(segmentId, NumberStyles.Integer, UsCulture);
        }

        private string GetSegmentPath(string playlist, string segmentExtension, int index)
        {
            var folder = Path.GetDirectoryName(playlist);

            var filename = Path.GetFileNameWithoutExtension(playlist);

            return Path.Combine(folder, filename + index.ToString("000", UsCulture) + segmentExtension);
        }
        
        protected override string GetAudioArguments(StreamState state)
        {
            var codec = state.OutputAudioCodec;

            if (codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
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
                args += " -ab " + bitrate.Value.ToString(UsCulture);
            }

            args += " " + GetAudioFilterParam(state, true);

            return args;
        }

        protected override string GetVideoArguments(StreamState state)
        {
            var codec = state.OutputVideoCodec;

            // See if we can save come cpu cycles by avoiding encoding
            if (codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                return IsH264(state.VideoStream) ? "-codec:v:0 copy -bsf:v h264_mp4toannexb" : "-codec:v:0 copy";
            }

            var keyFrameArg = string.Format(" -force_key_frames expr:gte(t,n_forced*{0})",
                state.SegmentLength.ToString(UsCulture));

            var hasGraphicalSubs = state.SubtitleStream != null && !state.SubtitleStream.IsTextSubtitleStream;

            var args = "-codec:v:0 " + codec + " " + GetVideoQualityParam(state, "libx264", true) + keyFrameArg;

            args += " -r 24 -g 24";

            // Add resolution params, if specified
            if (!hasGraphicalSubs)
            {
                args += GetOutputSizeParam(state, codec, false);
            }

            // This is for internal graphical subs
            if (hasGraphicalSubs)
            {
                args += GetGraphicalSubtitleParam(state, codec);
            }

            return args;
        }

        protected override string GetCommandLineArguments(string outputPath, string transcodingJobId, StreamState state, bool isEncoding)
        {
            // test url http://192.168.1.2:8096/mediabrowser/videos/233e8905d559a8f230db9bffd2ac9d6d/master.mpd?mediasourceid=233e8905d559a8f230db9bffd2ac9d6d&videocodec=h264&audiocodec=aac&maxwidth=1280&videobitrate=500000&audiobitrate=128000&profile=baseline&level=3
            // Good info on i-frames http://blog.streamroot.io/encode-multi-bitrate-videos-mpeg-dash-mse-based-media-players/

            var threads = GetNumberOfThreads(state, false);

            var inputModifier = GetInputModifier(state);

            // If isEncoding is true we're actually starting ffmpeg
            var startNumberParam = isEncoding ? GetStartNumber(state).ToString(UsCulture) : "0";

            var segmentFilename = Path.GetFileNameWithoutExtension(outputPath) + "%03d" + GetSegmentFileExtension(state);

            var args = string.Format("{0} -i {1} -map_metadata -1 -threads {2} {3} {4} -copyts {5} -f ssegment -segment_time {6} -segment_list_size {8} -segment_list \"{9}\" {10}",
                inputModifier,
                GetInputArgument(transcodingJobId, state),
                threads,
                GetMapArgs(state),
                GetVideoArguments(state),
                GetAudioArguments(state),
                state.SegmentLength.ToString(UsCulture),
                startNumberParam,
                state.HlsListSize.ToString(UsCulture),
                outputPath,
                segmentFilename
                ).Trim();

            return args;
        }

        /// <summary>
        /// Gets the segment file extension.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected override string GetSegmentFileExtension(StreamState state)
        {
            return ".mp4";
        }

        protected override TranscodingJobType TranscodingJobType
        {
            get
            {
                return TranscodingJobType.Dash;
            }
        }
    }
}
