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
    /// Class GetAudioStream
    /// </summary>
    [Route("/Audio/{Id}/stream.{Container}", "GET", Summary = "Gets an audio stream")]
    [Route("/Audio/{Id}/stream", "GET", Summary = "Gets an audio stream")]
    [Route("/Audio/{Id}/stream.{Container}", "HEAD", Summary = "Gets an audio stream")]
    [Route("/Audio/{Id}/stream", "HEAD", Summary = "Gets an audio stream")]
    public class GetAudioStream : StreamRequest
    {
    }

    /// <summary>
    /// Class AudioService
    /// </summary>
    // TODO: In order to autheneticate this in the future, Dlna playback will require updating
    //[Authenticated]
    public class AudioService : BaseProgressiveStreamingService
    {
        public AudioService(
            ILogger<AudioService> logger,
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
        public Task<object> Get(GetAudioStream request)
        {
            return ProcessRequest(request, false);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public Task<object> Head(GetAudioStream request)
        {
            return ProcessRequest(request, true);
        }

        protected override string GetCommandLineArguments(string outputPath, EncodingOptions encodingOptions, StreamState state, bool isEncoding)
        {
            return EncodingHelper.GetProgressiveAudioFullCommandLine(state, encodingOptions, outputPath);
        }
    }
}
