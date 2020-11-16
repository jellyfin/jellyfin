using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Models.StreamingDtos;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Http;
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
        private readonly IHttpContextAccessor _httpContextAccessor;

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
        /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
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
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor)
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
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Get audio stream.
        /// </summary>
        /// <param name="transcodingJobType">Transcoding job type.</param>
        /// <param name="streamingRequest">Streaming controller.Request dto.</param>
        /// <returns>A <see cref="Task"/> containing the resulting <see cref="ActionResult"/>.</returns>
        public async Task<ActionResult> GetAudioStream(
            TranscodingJobType transcodingJobType,
            StreamingRequestDto streamingRequest)
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                throw new ResourceNotFoundException(nameof(_httpContextAccessor.HttpContext));
            }

            bool isHeadRequest = _httpContextAccessor.HttpContext.Request.Method == System.Net.WebRequestMethods.Http.Head;
            var cancellationTokenSource = new CancellationTokenSource();

            using var state = await StreamingHelpers.GetStreamingState(
                    streamingRequest,
                    _httpContextAccessor.HttpContext.Request,
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
                StreamingHelpers.AddDlnaHeaders(state, _httpContextAccessor.HttpContext.Response.Headers, true, streamingRequest.StartTimeTicks, _httpContextAccessor.HttpContext.Request, _dlnaManager);

                await new ProgressiveFileCopier(state.DirectStreamProvider, null, _transcodingJobHelper, CancellationToken.None)
                    {
                        AllowEndOfFile = false
                    }.WriteToAsync(_httpContextAccessor.HttpContext.Response.Body, CancellationToken.None)
                    .ConfigureAwait(false);

                // TODO (moved from MediaBrowser.Api): Don't hardcode contentType
                return new FileStreamResult(_httpContextAccessor.HttpContext.Response.Body, MimeTypes.GetMimeType("file.ts")!);
            }

            // Static remote stream
            if (streamingRequest.Static && state.InputProtocol == MediaProtocol.Http)
            {
                StreamingHelpers.AddDlnaHeaders(state, _httpContextAccessor.HttpContext.Response.Headers, true, streamingRequest.StartTimeTicks, _httpContextAccessor.HttpContext.Request, _dlnaManager);

                var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
                return await FileStreamResponseHelpers.GetStaticRemoteStreamResult(state, isHeadRequest, httpClient, _httpContextAccessor.HttpContext).ConfigureAwait(false);
            }

            if (streamingRequest.Static && state.InputProtocol != MediaProtocol.File)
            {
                return new BadRequestObjectResult($"Input protocol {state.InputProtocol} cannot be streamed statically");
            }

            var outputPath = state.OutputFilePath;
            var outputPathExists = System.IO.File.Exists(outputPath);

            var transcodingJob = _transcodingJobHelper.GetTranscodingJob(outputPath, TranscodingJobType.Progressive);
            var isTranscodeCached = outputPathExists && transcodingJob != null;

            StreamingHelpers.AddDlnaHeaders(state, _httpContextAccessor.HttpContext.Response.Headers, streamingRequest.Static || isTranscodeCached, streamingRequest.StartTimeTicks, _httpContextAccessor.HttpContext.Request, _dlnaManager);

            // Static stream
            if (streamingRequest.Static)
            {
                var contentType = state.GetMimeType("." + state.OutputContainer, false) ?? state.GetMimeType(state.MediaPath);

                if (state.MediaSource.IsInfiniteStream)
                {
                    await new ProgressiveFileCopier(state.MediaPath, null, _transcodingJobHelper, CancellationToken.None)
                        {
                            AllowEndOfFile = false
                        }.WriteToAsync(_httpContextAccessor.HttpContext.Response.Body, CancellationToken.None)
                        .ConfigureAwait(false);

                    return new FileStreamResult(_httpContextAccessor.HttpContext.Response.Body, contentType);
                }

                return FileStreamResponseHelpers.GetStaticFileResult(
                    state.MediaPath,
                    contentType,
                    isHeadRequest,
                    _httpContextAccessor.HttpContext);
            }

            // Need to start ffmpeg (because media can't be returned directly)
            var encodingOptions = _serverConfigurationManager.GetEncodingOptions();
            var encodingHelper = new EncodingHelper(_mediaEncoder, _fileSystem, _subtitleEncoder, _configuration);
            var ffmpegCommandLineArguments = encodingHelper.GetProgressiveAudioFullCommandLine(state, encodingOptions, outputPath);
            return await FileStreamResponseHelpers.GetTranscodedFile(
                state,
                isHeadRequest,
                _httpContextAccessor.HttpContext,
                _transcodingJobHelper,
                ffmpegCommandLineArguments,
                transcodingJobType,
                cancellationTokenSource).ConfigureAwait(false);
        }
    }
}
