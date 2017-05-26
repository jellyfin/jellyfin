using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.System;

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
        public AudioService(IServerConfigurationManager serverConfig, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IFileSystem fileSystem, IDlnaManager dlnaManager, ISubtitleEncoder subtitleEncoder, IDeviceManager deviceManager, IMediaSourceManager mediaSourceManager, IZipClient zipClient, IJsonSerializer jsonSerializer, IAuthorizationContext authorizationContext, IImageProcessor imageProcessor, IEnvironmentInfo environmentInfo) : base(serverConfig, userManager, libraryManager, isoManager, mediaEncoder, fileSystem, dlnaManager, subtitleEncoder, deviceManager, mediaSourceManager, zipClient, jsonSerializer, authorizationContext, imageProcessor, environmentInfo)
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

        protected override string GetCommandLineArguments(string outputPath, StreamState state, bool isEncoding)
        {
            var encodingOptions = ApiEntryPoint.Instance.GetEncodingOptions();

            return EncodingHelper.GetProgressiveAudioFullCommandLine(state, encodingOptions, outputPath);
        }
    }
}
