using System.Threading.Tasks;
using Jellyfin.Common.Net;
using Jellyfin.Controller.Configuration;
using Jellyfin.Controller.Devices;
using Jellyfin.Controller.Dlna;
using Jellyfin.Controller.Library;
using Jellyfin.Controller.MediaEncoding;
using Jellyfin.Controller.Net;
using Jellyfin.Model.Configuration;
using Jellyfin.Model.IO;
using Jellyfin.Model.Serialization;
using Jellyfin.Model.Services;

namespace Jellyfin.Api.Playback.Progressive
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
            IHttpClient httpClient,
            IServerConfigurationManager serverConfig,
            IUserManager userManager,
            ILibraryManager libraryManager,
            IIsoManager isoManager,
            IMediaEncoder mediaEncoder,
            IFileSystem fileSystem,
            IDlnaManager dlnaManager,
            ISubtitleEncoder subtitleEncoder,
            IDeviceManager deviceManager,
            IMediaSourceManager mediaSourceManager,
            IJsonSerializer jsonSerializer,
            IAuthorizationContext authorizationContext)
                : base(httpClient,
                    serverConfig,
                    userManager,
                    libraryManager,
                    isoManager,
                    mediaEncoder,
                    fileSystem,
                    dlnaManager,
                    subtitleEncoder,
                    deviceManager,
                    mediaSourceManager,
                    jsonSerializer,
                    authorizationContext)
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
