using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.Playback.Progressive
{
    /// <summary>
    /// Class GetVideoStream
    /// </summary>
    [Route("/Videos/{Id}/stream.mpegts", "GET")]
    [Route("/Videos/{Id}/stream.ts", "GET")]
    [Route("/Videos/{Id}/stream.webm", "GET")]
    [Route("/Videos/{Id}/stream.asf", "GET")]
    [Route("/Videos/{Id}/stream.wmv", "GET")]
    [Route("/Videos/{Id}/stream.ogv", "GET")]
    [Route("/Videos/{Id}/stream.mp4", "GET")]
    [Route("/Videos/{Id}/stream.m4v", "GET")]
    [Route("/Videos/{Id}/stream.mkv", "GET")]
    [Route("/Videos/{Id}/stream.mpeg", "GET")]
    [Route("/Videos/{Id}/stream.mpg", "GET")]
    [Route("/Videos/{Id}/stream.avi", "GET")]
    [Route("/Videos/{Id}/stream.m2ts", "GET")]
    [Route("/Videos/{Id}/stream.3gp", "GET")]
    [Route("/Videos/{Id}/stream.wmv", "GET")]
    [Route("/Videos/{Id}/stream.wtv", "GET")]
    [Route("/Videos/{Id}/stream.mov", "GET")]
    [Route("/Videos/{Id}/stream.iso", "GET")]
    [Route("/Videos/{Id}/stream.flv", "GET")]
    [Route("/Videos/{Id}/stream.rm", "GET")]
    [Route("/Videos/{Id}/stream", "GET")]
    [Route("/Videos/{Id}/stream.ts", "HEAD")]
    [Route("/Videos/{Id}/stream.webm", "HEAD")]
    [Route("/Videos/{Id}/stream.asf", "HEAD")]
    [Route("/Videos/{Id}/stream.wmv", "HEAD")]
    [Route("/Videos/{Id}/stream.ogv", "HEAD")]
    [Route("/Videos/{Id}/stream.mp4", "HEAD")]
    [Route("/Videos/{Id}/stream.m4v", "HEAD")]
    [Route("/Videos/{Id}/stream.mkv", "HEAD")]
    [Route("/Videos/{Id}/stream.mpeg", "HEAD")]
    [Route("/Videos/{Id}/stream.mpg", "HEAD")]
    [Route("/Videos/{Id}/stream.avi", "HEAD")]
    [Route("/Videos/{Id}/stream.3gp", "HEAD")]
    [Route("/Videos/{Id}/stream.wmv", "HEAD")]
    [Route("/Videos/{Id}/stream.wtv", "HEAD")]
    [Route("/Videos/{Id}/stream.m2ts", "HEAD")]
    [Route("/Videos/{Id}/stream.mov", "HEAD")]
    [Route("/Videos/{Id}/stream.iso", "HEAD")]
    [Route("/Videos/{Id}/stream.flv", "HEAD")]
    [Route("/Videos/{Id}/stream", "HEAD")]
    public class GetVideoStream : VideoStreamRequest
    {

    }

    /// <summary>
    /// Class VideoService
    /// </summary>
    // TODO: In order to autheneticate this in the future, Dlna playback will require updating
    //[Authenticated]
    public class VideoService : BaseProgressiveStreamingService
    {
        public VideoService(
            ILogger<VideoService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IHttpClient httpClient,
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
                httpClient,
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
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public Task<object> Get(GetVideoStream request)
        {
            return ProcessRequest(request, false);
        }

        /// <summary>
        /// Heads the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public Task<object> Head(GetVideoStream request)
        {
            return ProcessRequest(request, true);
        }

        protected override string GetCommandLineArguments(string outputPath, EncodingOptions encodingOptions, StreamState state, bool isEncoding)
        {
            return EncodingHelper.GetProgressiveVideoFullCommandLine(state, encodingOptions, outputPath, GetDefaultEncoderPreset());
        }
    }
}
