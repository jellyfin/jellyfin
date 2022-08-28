using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Models.PlaybackDtos;
using Jellyfin.Api.Models.StreamingDtos;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Api.Helpers
{
    /// <summary>
    /// The stream response helpers.
    /// </summary>
    public static class FileStreamResponseHelpers
    {
        private static readonly List<string> HeadersToCopy = new List<string>()
        {
            HeaderNames.ContentLength,
            HeaderNames.ContentRange,
            HeaderNames.ContentDisposition,
            HeaderNames.ContentEncoding
        };

        private static readonly Regex PlaylistAttributeListPattern = new Regex("(?<AttributeName>[A-Z0-9-]+)=((\"(?<AttributeValue>.+?)\")|(?<AttributeValue>.+?))(?=,[^,=]+=|$)");
        private static readonly Uri DummyBaseUri = new Uri("http://example.com");

        /// <summary>
        /// Sets the key used for generating HMAC of segment URIs.
        /// </summary>
        public static string SegmentUriHmacKey { private get; set; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

        /// <summary>
        /// Returns a static file from a remote source.
        /// </summary>
        /// <param name="state">The current <see cref="StreamState"/>.</param>
        /// <param name="httpClient">The <see cref="HttpClient"/> making the remote request.</param>
        /// <param name="httpContext">The current http context.</param>
        /// <param name="accessToken">Access token used in the request.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A <see cref="Task{ActionResult}"/> containing the API response.</returns>
        public static async Task<ActionResult> GetStaticRemoteStreamResult(
            StreamState state,
            HttpClient httpClient,
            HttpContext httpContext,
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            Uri mediaPath = new Uri(state.MediaPath);
            if (!string.IsNullOrEmpty(state.Request.SegmentUri) && !string.IsNullOrEmpty(state.Request.SegmentToken))
            {
                if (!state.Request.SegmentToken.Equals(GenerateSegmentToken(state.MediaSource.Id, state.Request.SegmentUri!), StringComparison.Ordinal))
                {
                    throw new ArgumentException("Provided segment token for static stream is invalid");
                }

                mediaPath = new Uri(mediaPath, state.Request.SegmentUri);
            }

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, mediaPath);
            foreach (KeyValuePair<string, string> entry in state.RemoteHttpHeaders)
            {
                request.Headers.Add(entry.Key, entry.Value);
            }

            if (httpContext.Request.Headers.TryGetValue(HeaderNames.Range, out StringValues values))
            {
                request.Headers.Add(HeaderNames.Range, values.ToArray());
            }

            // Can't dispose the response as it's required up the call chain.
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            var contentType = response.Content.Headers.ContentType?.ToString() ?? MediaTypeNames.Text.Plain;

            foreach (var headerToCopy in HeadersToCopy)
            {
                if (response.Content.Headers.TryGetValues(headerToCopy, out var headerValue))
                {
                    httpContext.Response.Headers.Add(headerToCopy, headerValue.ToArray());
                }
            }

            if (".m3u8".Equals(new FileInfo(mediaPath.AbsolutePath).Extension, StringComparison.OrdinalIgnoreCase))
            {
                var playlistContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return await RewriteUrisInM3UPlaylist(state.Request.Id.ToString(), state.MediaSource.Id, accessToken, playlistContent);
            }

            httpContext.Response.StatusCode = (int)response.StatusCode;
            return new FileStreamResult(await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false), contentType);
        }

        private static string GetFileExtensionFromUri(string inputUri)
        {
            Uri? uri;
            if (!Uri.TryCreate(inputUri, UriKind.Absolute, out uri))
            {
                uri = new Uri(DummyBaseUri, inputUri);
            }

            return new FileInfo(uri.AbsolutePath).Extension;
        }

        private static string GenerateSegmentToken(string mediaSourceId, string segmentUri)
        {
            using (var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(SegmentUriHmacKey + mediaSourceId)))
            {
                var hash = hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(segmentUri));
                return Convert.ToBase64String(hash);
            }
        }

        private static async Task<string> MapSegmentUri(string itemId, string mediaSourceId, string accessToken, string uri)
        {
            var extension = GetFileExtensionFromUri(uri);
            var queryParameters = await new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "static", "true" },
                { "mediaSourceId", mediaSourceId },
                { "api_key", accessToken },
                { "segmentUri", uri },
                { "segmentToken", GenerateSegmentToken(mediaSourceId, uri) }
            }).ReadAsStringAsync();
            return $"/Videos/{itemId}/stream{extension}?{queryParameters}";
        }

        internal static async Task<ContentResult> RewriteUrisInM3UPlaylist(string itemId, string mediaSourceId, string accessToken, string playlistContent)
        {
            StringReader reader = new StringReader(playlistContent);
            string? line;
            var outputLines = new List<string>();
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.StartsWith("#EXT", StringComparison.Ordinal) && line.Contains(":", StringComparison.Ordinal))
                {
                    // Parse lines containing tags with attributes, e.g. #EXT-X-TAG:ATTR1=1,ATTR2=value2,ATTR3="Value3"...
                    StringBuilder sb = new StringBuilder();
                    var tagComponents = line.Split(":", 2, StringSplitOptions.TrimEntries);
                    sb.Append(tagComponents[0] + ":");
                    var matches = PlaylistAttributeListPattern.Matches(tagComponents[1]);
                    if (matches.Count == 0)
                    {
                        // If we fail to parse the attributes, leave them as-is.
                        sb.Append(tagComponents[1]);
                    }
                    else
                    {
                        List<string> attributeList = new List<string>();
                        foreach (Match match in matches)
                        {
                            var key = match.Groups["AttributeName"].Value;
                            var value = match.Groups["AttributeValue"].Value;
                            if ("URI".Equals(key, StringComparison.Ordinal))
                            {
                                // Extract URI attributes and re-write them.
                                value = await MapSegmentUri(itemId, mediaSourceId, accessToken, value);
                            }

                            attributeList.Add($"{key}=\"{value}\"");
                        }

                        // Join attributes together.
                        sb.Append(string.Join(",", attributeList));
                    }

                    outputLines.Add(sb.ToString());
                }
                else if (!line.StartsWith("#", StringComparison.InvariantCultureIgnoreCase) && line.Trim().Length > 0)
                {
                    // Lines containing no tags represent segment URI; re-write them.
                    outputLines.Add(await MapSegmentUri(itemId, mediaSourceId, accessToken, line));
                }
                else
                {
                    // Leave everything else intact.
                    outputLines.Add(line);
                }
            }

            return new ContentResult()
            {
                Content = string.Join("\n", outputLines)
            };
        }

        /// <summary>
        /// Returns a static file from the server.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="contentType">The content type of the file.</param>
        /// <returns>An <see cref="ActionResult"/> the file.</returns>
        public static ActionResult GetStaticFileResult(
            string path,
            string contentType)
        {
            return new PhysicalFileResult(path, contentType) { EnableRangeProcessing = true };
        }

        /// <summary>
        /// Returns a transcoded file from the server.
        /// </summary>
        /// <param name="state">The current <see cref="StreamState"/>.</param>
        /// <param name="isHeadRequest">Whether the current request is a HTTP HEAD request so only the headers get returned.</param>
        /// <param name="httpContext">The current http context.</param>
        /// <param name="transcodingJobHelper">The <see cref="TranscodingJobHelper"/> singleton.</param>
        /// <param name="ffmpegCommandLineArguments">The command line arguments to start ffmpeg.</param>
        /// <param name="transcodingJobType">The <see cref="TranscodingJobType"/>.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns>A <see cref="Task{ActionResult}"/> containing the transcoded file.</returns>
        public static async Task<ActionResult> GetTranscodedFile(
            StreamState state,
            bool isHeadRequest,
            HttpContext httpContext,
            TranscodingJobHelper transcodingJobHelper,
            string ffmpegCommandLineArguments,
            TranscodingJobType transcodingJobType,
            CancellationTokenSource cancellationTokenSource)
        {
            // Use the command line args with a dummy playlist path
            var outputPath = state.OutputFilePath;

            httpContext.Response.Headers[HeaderNames.AcceptRanges] = "none";

            var contentType = state.GetMimeType(outputPath);

            // Headers only
            if (isHeadRequest)
            {
                httpContext.Response.Headers[HeaderNames.ContentType] = contentType;
                return new OkResult();
            }

            var transcodingLock = transcodingJobHelper.GetTranscodingLock(outputPath);
            await transcodingLock.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            try
            {
                TranscodingJobDto? job;
                if (!File.Exists(outputPath))
                {
                    job = await transcodingJobHelper.StartFfMpeg(state, outputPath, ffmpegCommandLineArguments, httpContext.Request, transcodingJobType, cancellationTokenSource).ConfigureAwait(false);
                }
                else
                {
                    job = transcodingJobHelper.OnTranscodeBeginRequest(outputPath, TranscodingJobType.Progressive);
                    state.Dispose();
                }

                var stream = new ProgressiveFileStream(outputPath, job, transcodingJobHelper);
                return new FileStreamResult(stream, contentType);
            }
            finally
            {
                transcodingLock.Release();
            }
        }
    }
}
