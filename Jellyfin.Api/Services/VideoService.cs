using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Helpers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model;
using MediaBrowser.Model.MediaInfo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Services;

/// <summary>
/// Video Stream processing service.
/// </summary>
public class VideoService : IVideoService
{
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly IServerConfigurationManager _serverConfigurationManager;
    private readonly ITranscodeManager _transcodeManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EncodingHelper _encodingHelper;
    private readonly IStreamingHelper _streamingHelper;

    private readonly TranscodingJobType _transcodingJobType = TranscodingJobType.Progressive;

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoService"/> class.
    /// </summary>
    /// <param name="mediaSourceManager">Instance of <see cref="IMediaSourceManager"/>.</param>
    /// <param name="serverConfigurationManager">Instance of <see cref="IServerConfigurationManager"/>.</param>
    /// <param name="transcodeManager">Instance of <see cref="ITranscodeManager"/>.</param>
    /// <param name="httpClientFactory">Instance of <see cref="IHttpClientFactory"/>.</param>
    /// <param name="encodingHelper">Instance of <see cref="EncodingHelper"/>.</param>
    /// <param name="streamingHelper">Instance of <see cref="IStreamingHelper"/>.</param>
    public VideoService(IMediaSourceManager mediaSourceManager, IServerConfigurationManager serverConfigurationManager, ITranscodeManager transcodeManager, IHttpClientFactory httpClientFactory, EncodingHelper encodingHelper, IStreamingHelper streamingHelper)
    {
        _mediaSourceManager = mediaSourceManager;
        _serverConfigurationManager = serverConfigurationManager;
        _transcodeManager = transcodeManager;
        _httpClientFactory = httpClientFactory;
        _encodingHelper = encodingHelper;
        _streamingHelper = streamingHelper;
    }

    /// <summary>
    /// Gets the video stream to return.
    /// </summary>
    /// <param name="request">the web request.</param>
    /// <param name="httpRequest">the http request.</param>
    /// <param name="httpContext">the http context.</param>
    /// <returns>web response.</returns>
    public async Task<ActionResult> GetVideoStreamAsync(VideoRequestDto request, HttpRequest httpRequest, HttpContext httpContext)
    {
        var isHeadRequest = httpRequest.Method == System.Net.WebRequestMethods.Http.Head;
        // CTS lifecycle is managed internally.
        var cancellationTokenSource = new CancellationTokenSource();

        var state = await _streamingHelper.GetStreamingState(
                request,
                httpContext,
                _encodingHelper,
                _transcodingJobType,
                cancellationTokenSource.Token)
            .ConfigureAwait(false);

        if (request.Static)
        {
            if (state.DirectStreamProvider is not null)
            {
                var liveStreamInfo = _mediaSourceManager.GetLiveStreamInfo(request.LiveStreamId);
                if (liveStreamInfo is null)
                {
                    return new NotFoundResult();
                }

                var liveStream = new ProgressiveFileStream(liveStreamInfo.GetStream());
                // TODO (moved from MediaBrowser.Api): Don't hardcode contentType
                return new FileStreamResult(liveStream, MimeTypes.GetMimeType("file.ts"));
            }

            // Static remote stream
            if (state.InputProtocol == MediaProtocol.Http)
            {
                var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
                return await FileStreamResponseHelpers.GetStaticRemoteStreamResult(state, httpClient, httpContext).ConfigureAwait(false);
            }

            if (state.InputProtocol != MediaProtocol.File)
            {
                return new BadRequestObjectResult($"Input protocol {state.InputProtocol} cannot be streamed statically");
            }

            // Static stream
            if (!state.MediaSource.IsDiscSource())
            {
                var contentType = state.GetMimeType("." + state.OutputContainer, false) ?? state.GetMimeType(state.MediaPath);

                if (state.MediaSource.IsInfiniteStream)
                {
                    var liveStream = new ProgressiveFileStream(state.MediaPath, null, _transcodeManager);
                    return new FileStreamResult(liveStream, contentType);
                }

                return FileStreamResponseHelpers.GetStaticFileResult(
                    state.MediaPath,
                    contentType);
            }
        }

        // Need to start ffmpeg (because media can't be returned directly)
        var encodingOptions = _serverConfigurationManager.GetEncodingOptions();
        var ffmpegCommandLineArguments = _encodingHelper.GetProgressiveVideoFullCommandLine(state, encodingOptions, "superfast");
        return await FileStreamResponseHelpers.GetTranscodedFile(
            state,
            isHeadRequest,
            httpContext,
            _transcodeManager,
            ffmpegCommandLineArguments,
            _transcodingJobType,
            cancellationTokenSource).ConfigureAwait(false);
    }
}
