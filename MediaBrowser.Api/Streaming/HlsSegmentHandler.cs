using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Streaming
{
    /// <summary>
    /// Class HlsSegmentHandler
    /// </summary>
    public class HlsSegmentHandler : BaseHandler<Kernel>
    {
        /// <summary>
        /// The segment file prefix
        /// </summary>
        public const string SegmentFilePrefix = "segment-";

        /// <summary>
        /// Handleses the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            const string url = "/api/" + SegmentFilePrefix;

            return request.Url.LocalPath.IndexOf(url, StringComparison.OrdinalIgnoreCase) != -1;
        }

        /// <summary>
        /// Writes the response to output stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="responseInfo">The response info.</param>
        /// <param name="contentLength">Length of the content.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        protected override Task WriteResponseToOutputStream(Stream stream, ResponseInfo responseInfo, long? contentLength)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the response info.
        /// </summary>
        /// <returns>Task{ResponseInfo}.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        protected override Task<ResponseInfo> GetResponseInfo()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Processes the request.
        /// </summary>
        /// <param name="ctx">The CTX.</param>
        /// <returns>Task.</returns>
        public override async Task ProcessRequest(HttpListenerContext ctx)
        {
            var path = Path.GetFileName(ctx.Request.Url.LocalPath);

            path = Path.Combine(Kernel.ApplicationPaths.FFMpegStreamCachePath, path);

            var playlistFilename = Path.GetFileNameWithoutExtension(path).Substring(SegmentFilePrefix.Length);
            playlistFilename = playlistFilename.Substring(0, playlistFilename.Length - 3);

            var playlistPath = Path.Combine(Path.GetDirectoryName(path), playlistFilename + ".m3u8");

            Plugin.Instance.OnTranscodeBeginRequest(playlistPath, TranscodingJobType.Hls);

            try
            {
                await new StaticFileHandler(Kernel) { Path = path }.ProcessRequest(ctx).ConfigureAwait(false);
            }
            finally
            {
                Plugin.Instance.OnTranscodeEndRequest(playlistPath, TranscodingJobType.Hls);
            }
        }
    }
}
