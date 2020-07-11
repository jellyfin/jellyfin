using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Helpers;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Api.Controllers
{

    /// <summary>
    /// The audio controller.
    /// </summary>
    public class AudioController : BaseJellyfinApiController
    {
        private readonly IDlnaManager _dlnaManager;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioController"/> class.
        /// </summary>
        /// <param name="dlnaManager">Instance of the <see cref="IDlnaManager"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{AuidoController}"/> interface.</param>
        public AudioController(IDlnaManager dlnaManager, ILogger<AudioController> logger)
        {
            _dlnaManager = dlnaManager;
            _logger = logger;
        }

        [HttpGet("{id}/stream.{container}")]
        [HttpGet("{id}/stream")]
        [HttpHead("{id}/stream.{container}")]
        [HttpGet("{id}/stream")]
        public async Task<ActionResult> GetAudioStream(
            [FromRoute] string id,
            [FromRoute] string container,
            [FromQuery] bool Static,
            [FromQuery] string tag)
        {
            bool isHeadRequest = Request.Method == System.Net.WebRequestMethods.Http.Head;

            var cancellationTokenSource = new CancellationTokenSource();

            var state = await GetState(request, cancellationTokenSource.Token).ConfigureAwait(false);

            if (Static && state.DirectStreamProvider != null)
            {
                StreamingHelpers.AddDlnaHeaders(state, Response.Headers, true, Request, _dlnaManager);

                using (state)
                {
                    var outputHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    // TODO: Don't hardcode this
                    outputHeaders[HeaderNames.ContentType] = MimeTypes.GetMimeType("file.ts");

                    return new ProgressiveFileCopier(state.DirectStreamProvider, outputHeaders, null, _logger, CancellationToken.None)
                    {
                        AllowEndOfFile = false
                    };
                }
            }

            // Static remote stream
            if (Static && state.InputProtocol == MediaProtocol.Http)
            {
                StreamingHelpers.AddDlnaHeaders(state, Response.Headers, true, Request, _dlnaManager);

                using (state)
                {
                    return await GetStaticRemoteStreamResult(state, responseHeaders, isHeadRequest, cancellationTokenSource).ConfigureAwait(false);
                }
            }

            if (Static && state.InputProtocol != MediaProtocol.File)
            {
                throw new ArgumentException(string.Format($"Input protocol {state.InputProtocol} cannot be streamed statically."));
            }

            var outputPath = state.OutputFilePath;
            var outputPathExists = File.Exists(outputPath);

            var transcodingJob = TranscodingJobHelper.GetTranscodingJob(outputPath, TranscodingJobType.Progressive);
            var isTranscodeCached = outputPathExists && transcodingJob != null;

            StreamingHelpers.AddDlnaHeaders(state, Response.Headers, Static || isTranscodeCached, Request, _dlnaManager);

            // Static stream
            if (Static)
            {
                var contentType = state.GetMimeType("." + state.OutputContainer, false) ?? state.GetMimeType(state.MediaPath);

                using (state)
                {
                    if (state.MediaSource.IsInfiniteStream)
                    {
                        var outputHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            [HeaderNames.ContentType] = contentType
                        };


                        return new ProgressiveFileCopier(FileSystem, state.MediaPath, outputHeaders, null, _logger, CancellationToken.None)
                        {
                            AllowEndOfFile = false
                        };
                    }

                    TimeSpan? cacheDuration = null;

                    if (!string.IsNullOrEmpty(tag))
                    {
                        cacheDuration = TimeSpan.FromDays(365);
                    }

                    return await ResultFactory.GetStaticFileResult(Request, new StaticFileResultOptions
                    {
                        ResponseHeaders = responseHeaders,
                        ContentType = contentType,
                        IsHeadRequest = isHeadRequest,
                        Path = state.MediaPath,
                        CacheDuration = cacheDuration

                    }).ConfigureAwait(false);
                }
            }

            //// Not static but transcode cache file exists
            //if (isTranscodeCached && state.VideoRequest == null)
            //{
            //    var contentType = state.GetMimeType(outputPath);

            //    try
            //    {
            //        if (transcodingJob != null)
            //        {
            //            ApiEntryPoint.Instance.OnTranscodeBeginRequest(transcodingJob);
            //        }

            //        return await ResultFactory.GetStaticFileResult(Request, new StaticFileResultOptions
            //        {
            //            ResponseHeaders = responseHeaders,
            //            ContentType = contentType,
            //            IsHeadRequest = isHeadRequest,
            //            Path = outputPath,
            //            FileShare = FileShare.ReadWrite,
            //            OnComplete = () =>
            //            {
            //                if (transcodingJob != null)
            //                {
            //                    ApiEntryPoint.Instance.OnTranscodeEndRequest(transcodingJob);
            //                }
            //            }

            //        }).ConfigureAwait(false);
            //    }
            //    finally
            //    {
            //        state.Dispose();
            //    }
            //}

            // Need to start ffmpeg
            try
            {
                return await GetStreamResult(request, state, responseHeaders, isHeadRequest, cancellationTokenSource).ConfigureAwait(false);
            }
            catch
            {
                state.Dispose();

                throw;
            }
        }
    }
}
