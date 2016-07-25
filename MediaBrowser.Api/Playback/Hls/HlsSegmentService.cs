using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using ServiceStack;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Playback.Hls
{
    /// <summary>
    /// Class GetHlsAudioSegment
    /// </summary>
    [Route("/Audio/{Id}/hls/{SegmentId}/stream.mp3", "GET")]
    [Route("/Audio/{Id}/hls/{SegmentId}/stream.aac", "GET")]
    [Api(Description = "Gets an Http live streaming segment file. Internal use only.")]
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
    [Api(Description = "Gets an Http live streaming segment file. Internal use only.")]
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
    [Api(Description = "Stops an encoding process")]
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
    [Route("/Videos/{Id}/hls/{PlaylistId}/{SegmentId}.ts", "GET")]
    [Api(Description = "Gets an Http live streaming segment file. Internal use only.")]
    public class GetHlsVideoSegmentLegacy : VideoStreamRequest
    {
        // TODO: Deprecate with new iOS app

        public string PlaylistId { get; set; }

        /// <summary>
        /// Gets or sets the segment id.
        /// </summary>
        /// <value>The segment id.</value>
        public string SegmentId { get; set; }
    }

    public class HlsSegmentService : BaseApiService
    {
        private readonly IServerApplicationPaths _appPaths;
        private readonly IServerConfigurationManager _config;

        public HlsSegmentService(IServerApplicationPaths appPaths, IServerConfigurationManager config)
        {
            _appPaths = appPaths;
            _config = config;
        }

        public Task<object> Get(GetHlsPlaylistLegacy request)
        {
            var file = request.PlaylistId + Path.GetExtension(Request.PathInfo);
            file = Path.Combine(_appPaths.TranscodingTempPath, file);

            return GetFileResult(file, file);
        }

        public void Delete(StopEncodingProcess request)
        {
            ApiEntryPoint.Instance.KillTranscodingJobs(request.DeviceId, request.PlaySessionId, path => true);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public Task<object> Get(GetHlsVideoSegmentLegacy request)
        {
            var file = request.SegmentId + Path.GetExtension(Request.PathInfo);
            file = Path.Combine(_config.ApplicationPaths.TranscodingTempPath, file);

            var normalizedPlaylistId = request.PlaylistId.Replace("-low", string.Empty);

            var playlistPath = Directory.EnumerateFiles(_config.ApplicationPaths.TranscodingTempPath, "*")
                .FirstOrDefault(i => string.Equals(Path.GetExtension(i), ".m3u8", StringComparison.OrdinalIgnoreCase) && i.IndexOf(normalizedPlaylistId, StringComparison.OrdinalIgnoreCase) != -1);

            return GetFileResult(file, playlistPath);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetHlsAudioSegmentLegacy request)
        {
            // TODO: Deprecate with new iOS app
            var file = request.SegmentId + Path.GetExtension(Request.PathInfo);
            file = Path.Combine(_appPaths.TranscodingTempPath, file);

            return ResultFactory.GetStaticFileResult(Request, file, FileShare.ReadWrite).Result;
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
