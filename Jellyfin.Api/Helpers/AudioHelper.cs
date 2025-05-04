using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Audio helper.
/// </summary>
public class AudioHelper
{
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly IServerConfigurationManager _serverConfigurationManager;
    private readonly IMediaEncoder _mediaEncoder;
    private readonly ITranscodeManager _transcodeManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly EncodingHelper _encodingHelper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioHelper"/> class.
    /// </summary>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    /// <param name="transcodeManager">Instance of <see cref="ITranscodeManager"/> interface.</param>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
    /// <param name="encodingHelper">Instance of <see cref="EncodingHelper"/>.</param>
    public AudioHelper(
        IUserManager userManager,
        ILibraryManager libraryManager,
        IMediaSourceManager mediaSourceManager,
        IServerConfigurationManager serverConfigurationManager,
        IMediaEncoder mediaEncoder,
        ITranscodeManager transcodeManager,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        EncodingHelper encodingHelper)
    {
        _userManager = userManager;
        _libraryManager = libraryManager;
        _mediaSourceManager = mediaSourceManager;
        _serverConfigurationManager = serverConfigurationManager;
        _mediaEncoder = mediaEncoder;
        _transcodeManager = transcodeManager;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _encodingHelper = encodingHelper;
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
        if (_httpContextAccessor.HttpContext is null)
        {
            throw new ResourceNotFoundException(nameof(_httpContextAccessor.HttpContext));
        }

        bool isHeadRequest = _httpContextAccessor.HttpContext.Request.Method == System.Net.WebRequestMethods.Http.Head;

        // CTS lifecycle is managed internally.
        var cancellationTokenSource = new CancellationTokenSource();

        using var state = await StreamingHelpers.GetStreamingState(
                streamingRequest,
                _httpContextAccessor.HttpContext,
                _mediaSourceManager,
                _userManager,
                _libraryManager,
                _serverConfigurationManager,
                _mediaEncoder,
                _encodingHelper,
                _transcodeManager,
                transcodingJobType,
                cancellationTokenSource.Token)
            .ConfigureAwait(false);

        if (streamingRequest.Static && state.DirectStreamProvider is not null)
        {
            var liveStreamInfo = _mediaSourceManager.GetLiveStreamInfo(streamingRequest.LiveStreamId);
            if (liveStreamInfo is null)
            {
                throw new FileNotFoundException();
            }

            var liveStream = new ProgressiveFileStream(liveStreamInfo.GetStream());
            // TODO (moved from MediaBrowser.Api): Don't hardcode contentType
            return new FileStreamResult(liveStream, MimeTypes.GetMimeType("file.ts"));
        }

        // Static remote stream
        if (streamingRequest.Static && state.InputProtocol == MediaProtocol.Http)
        {
            var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
            return await FileStreamResponseHelpers.GetStaticRemoteStreamResult(state, httpClient, _httpContextAccessor.HttpContext).ConfigureAwait(false);
        }

        if (streamingRequest.Static && state.InputProtocol != MediaProtocol.File)
        {
            return new BadRequestObjectResult($"Input protocol {state.InputProtocol} cannot be streamed statically");
        }

        var outputPath = state.OutputFilePath;

        // Static stream
        if (streamingRequest.Static)
        {
            var contentType = state.GetMimeType("." + state.OutputContainer, false) ?? state.GetMimeType(state.MediaPath);

            if (state.MediaSource.IsInfiniteStream)
            {
                var stream = new ProgressiveFileStream(state.MediaPath, null, _transcodeManager);
                return new FileStreamResult(stream, contentType);
            }

            return FileStreamResponseHelpers.GetStaticFileResult(
                state.MediaPath,
                contentType);
        }

        // Need to start ffmpeg (because media can't be returned directly)
        var encodingOptions = _serverConfigurationManager.GetEncodingOptions();
        var ffmpegCommandLineArguments = _encodingHelper.GetProgressiveAudioFullCommandLine(state, encodingOptions, outputPath);
        return await FileStreamResponseHelpers.GetTranscodedFile(
            state,
            isHeadRequest,
            _httpContextAccessor.HttpContext,
            _transcodeManager,
            ffmpegCommandLineArguments,
            transcodingJobType,
            cancellationTokenSource).ConfigureAwait(false);
    }
}
