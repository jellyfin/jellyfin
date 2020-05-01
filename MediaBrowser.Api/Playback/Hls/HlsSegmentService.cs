using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.Playback.Hls
{
    /// <summary>
    /// Class GetHlsAudioSegment
    /// </summary>
    // Can't require authentication just yet due to seeing some requests come from Chrome without full query string
    //[Authenticated]
    [Route("/Audio/{Id}/hls/{SegmentId}/stream.mp3", "GET")]
    [Route("/Audio/{Id}/hls/{SegmentId}/stream.aac", "GET")]
    public class GetHlsAudioSegmentLegacy
    {
        // TODO: Deprecate with new iOS app

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the segment id.
        /// </summary>
        /// <value>The segment id.</value>
        public string SegmentId { get; set; }
    }

    /// <summary>
    /// Class GetHlsVideoSegment
    /// </summary>
    [Route("/Videos/{Id}/hls/{PlaylistId}/stream.m3u8", "GET")]
    [Authenticated]
    public class GetHlsPlaylistLegacy
    {
        // TODO: Deprecate with new iOS app

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }

        public string PlaylistId { get; set; }
    }

    [Route("/Videos/ActiveEncodings", "DELETE")]
    [Authenticated]
    public class StopEncodingProcess
    {
        [ApiMember(Name = "DeviceId", Description = "The device id of the client requesting. Used to stop encoding processes when needed.", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "DELETE")]
        public string DeviceId { get; set; }

        [ApiMember(Name = "PlaySessionId", Description = "The play session id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "DELETE")]
        public string PlaySessionId { get; set; }
    }

    /// <summary>
    /// Class GetHlsVideoSegment
    /// </summary>
    // Can't require authentication just yet due to seeing some requests come from Chrome without full query string
    //[Authenticated]
    [Route("/Videos/{Id}/hls/{PlaylistId}/{SegmentId}.{SegmentContainer}", "GET")]
    public class GetHlsVideoSegmentLegacy : VideoStreamRequest
    {
        public string PlaylistId { get; set; }

        /// <summary>
        /// Gets or sets the segment id.
        /// </summary>
        /// <value>The segment id.</value>
        public string SegmentId { get; set; }
    }

    public class HlsSegmentService : BaseApiService
    {
        private readonly IFileSystem _fileSystem;

        public HlsSegmentService(
            ILogger<HlsSegmentService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IFileSystem fileSystem)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _fileSystem = fileSystem;
        }

        public Task<object> Get(GetHlsPlaylistLegacy request)
        {
            var file = request.PlaylistId + Path.GetExtension(Request.PathInfo);
            file = Path.Combine(ServerConfigurationManager.GetTranscodePath(), file);

            return GetFileResult(file, file);
        }

        public Task Delete(StopEncodingProcess request)
        {
            return ApiEntryPoint.Instance.KillTranscodingJobs(request.DeviceId, request.PlaySessionId, path => true);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public Task<object> Get(GetHlsVideoSegmentLegacy request)
        {
            var file = request.SegmentId + Path.GetExtension(Request.PathInfo);
            var transcodeFolderPath = ServerConfigurationManager.GetTranscodePath();

            file = Path.Combine(transcodeFolderPath, file);

            var normalizedPlaylistId = request.PlaylistId;

            var playlistPath = _fileSystem.GetFilePaths(transcodeFolderPath)
                .FirstOrDefault(i => string.Equals(Path.GetExtension(i), ".m3u8", StringComparison.OrdinalIgnoreCase) && i.IndexOf(normalizedPlaylistId, StringComparison.OrdinalIgnoreCase) != -1);

            return GetFileResult(file, playlistPath);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public Task<object> Get(GetHlsAudioSegmentLegacy request)
        {
            // TODO: Deprecate with new iOS app
            var file = request.SegmentId + Path.GetExtension(Request.PathInfo);
            file = Path.Combine(ServerConfigurationManager.GetTranscodePath(), file);

            return ResultFactory.GetStaticFileResult(Request, file, FileShare.ReadWrite);
        }

        private Task<object> GetFileResult(string path, string playlistPath)
        {
            var transcodingJob = ApiEntryPoint.Instance.OnTranscodeBeginRequest(playlistPath, TranscodingJobType.Hls);

            return ResultFactory.GetStaticFileResult(Request, new StaticFileResultOptions
            {
                Path = path,
                FileShare = FileShare.ReadWrite,
                OnComplete = () =>
                {
                    if (transcodingJob != null)
                    {
                        ApiEntryPoint.Instance.OnTranscodeEndRequest(transcodingJob);
                    }
                }
            });
        }
    }
}
