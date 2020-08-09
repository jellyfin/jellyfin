using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Models.StreamingDtos;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Jellyfin.Api.Helpers
{
    /// <summary>
    /// Audio helper.
    /// </summary>
    public class AudioHelper
    {
        private readonly IDlnaManager _dlnaManager;
        private readonly IAuthorizationContext _authContext;
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IServerConfigurationManager _serverConfigurationManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IFileSystem _fileSystem;
        private readonly ISubtitleEncoder _subtitleEncoder;
        private readonly IConfiguration _configuration;
        private readonly IDeviceManager _deviceManager;
        private readonly TranscodingJobHelper _transcodingJobHelper;
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioHelper"/> class.
        /// </summary>
        /// <param name="dlnaManager">Instance of the <see cref="IDlnaManager"/> interface.</param>
        /// <param name="authContext">Instance of the <see cref="IAuthorizationContext"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="subtitleEncoder">Instance of the <see cref="ISubtitleEncoder"/> interface.</param>
        /// <param name="configuration">Instance of the <see cref="IConfiguration"/> interface.</param>
        /// <param name="deviceManager">Instance of the <see cref="IDeviceManager"/> interface.</param>
        /// <param name="transcodingJobHelper">Instance of <see cref="TranscodingJobHelper"/>.</param>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        public AudioHelper(
            IDlnaManager dlnaManager,
            IAuthorizationContext authContext,
            IUserManager userManager,
            ILibraryManager libraryManager,
            IMediaSourceManager mediaSourceManager,
            IServerConfigurationManager serverConfigurationManager,
            IMediaEncoder mediaEncoder,
            IFileSystem fileSystem,
            ISubtitleEncoder subtitleEncoder,
            IConfiguration configuration,
            IDeviceManager deviceManager,
            TranscodingJobHelper transcodingJobHelper,
            IHttpClientFactory httpClientFactory)
        {
            _dlnaManager = dlnaManager;
            _authContext = authContext;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _mediaSourceManager = mediaSourceManager;
            _serverConfigurationManager = serverConfigurationManager;
            _mediaEncoder = mediaEncoder;
            _fileSystem = fileSystem;
            _subtitleEncoder = subtitleEncoder;
            _configuration = configuration;
            _deviceManager = deviceManager;
            _transcodingJobHelper = transcodingJobHelper;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Get audio stream.
        /// </summary>
        /// <param name="controller">Requesting controller.</param>
        /// <param name="transcodingJobType">Transcoding job type.</param>
        /// <param name="streamingRequest">Streaming controller.Request dto.</param>
        /// <returns>A <see cref="Task"/> containing the resulting <see cref="ActionResult"/>.</returns>
        public async Task<ActionResult> GetAudioStream(
            BaseJellyfinApiController controller,
            TranscodingJobType transcodingJobType,
            StreamingRequestDto streamingRequest)
        {
            bool isHeadRequest = controller.Request.Method == System.Net.WebRequestMethods.Http.Head;
            var cancellationTokenSource = new CancellationTokenSource();

            using var state = await StreamingHelpers.GetStreamingState(
                    streamingRequest,
                    controller.Request,
                    _authContext,
                    _mediaSourceManager,
                    _userManager,
                    _libraryManager,
                    _serverConfigurationManager,
                    _mediaEncoder,
                    _fileSystem,
                    _subtitleEncoder,
                    _configuration,
                    _dlnaManager,
                    _deviceManager,
                    _transcodingJobHelper,
                    transcodingJobType,
                    cancellationTokenSource.Token)
                .ConfigureAwait(false);

            if (streamingRequest.Static && state.DirectStreamProvider != null)
            {
                StreamingHelpers.AddDlnaHeaders(state, controller.Response.Headers, true, streamingRequest.StartTimeTicks, controller.Request, _dlnaManager);

                await new ProgressiveFileCopier(state.DirectStreamProvider, null, _transcodingJobHelper, CancellationToken.None)
                    {
                        AllowEndOfFile = false
                    }.WriteToAsync(controller.Response.Body, CancellationToken.None)
                    .ConfigureAwait(false);

                // TODO (moved from MediaBrowser.Api): Don't hardcode contentType
                return controller.File(controller.Response.Body, MimeTypes.GetMimeType("file.ts")!);
            }

            // Static remote stream
            if (streamingRequest.Static && state.InputProtocol == MediaProtocol.Http)
            {
                StreamingHelpers.AddDlnaHeaders(state, controller.Response.Headers, true, streamingRequest.StartTimeTicks, controller.Request, _dlnaManager);

                using var httpClient = _httpClientFactory.CreateClient();
                return await FileStreamResponseHelpers.GetStaticRemoteStreamResult(state, isHeadRequest, controller, httpClient).ConfigureAwait(false);
            }

            if (streamingRequest.Static && state.InputProtocol != MediaProtocol.File)
            {
                return controller.BadRequest($"Input protocol {state.InputProtocol} cannot be streamed statically");
            }

            var outputPath = state.OutputFilePath;
            var outputPathExists = System.IO.File.Exists(outputPath);

            var transcodingJob = _transcodingJobHelper.GetTranscodingJob(outputPath, TranscodingJobType.Progressive);
            var isTranscodeCached = outputPathExists && transcodingJob != null;

            StreamingHelpers.AddDlnaHeaders(state, controller.Response.Headers, streamingRequest.Static || isTranscodeCached, streamingRequest.StartTimeTicks, controller.Request, _dlnaManager);

            // Static stream
            if (streamingRequest.Static)
            {
                var contentType = state.GetMimeType("." + state.OutputContainer, false) ?? state.GetMimeType(state.MediaPath);

                if (state.MediaSource.IsInfiniteStream)
                {
                    await new ProgressiveFileCopier(state.MediaPath, null, _transcodingJobHelper, CancellationToken.None)
                        {
                            AllowEndOfFile = false
                        }.WriteToAsync(controller.Response.Body, CancellationToken.None)
                        .ConfigureAwait(false);

                    return controller.File(controller.Response.Body, contentType);
                }

                return FileStreamResponseHelpers.GetStaticFileResult(
                    state.MediaPath,
                    contentType,
                    isHeadRequest,
                    controller);
            }

            // Need to start ffmpeg (because media can't be returned directly)
            var encodingOptions = _serverConfigurationManager.GetEncodingOptions();
            var encodingHelper = new EncodingHelper(_mediaEncoder, _fileSystem, _subtitleEncoder, _configuration);
            var ffmpegCommandLineArguments = encodingHelper.GetProgressiveAudioFullCommandLine(state, encodingOptions, outputPath);
            return await FileStreamResponseHelpers.GetTranscodedFile(
                state,
                isHeadRequest,
                controller,
                _transcodingJobHelper,
                ffmpegCommandLineArguments,
                controller.Request,
                transcodingJobType,
                cancellationTokenSource).ConfigureAwait(false);
        }
    }
}
