using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Emby.Server.Implementations.HttpClientManager
{
    /// <summary>
    /// Class HttpClientManager
    /// </summary>
    public class HttpClientManager : IHttpClient
    {
        private readonly ILogger _logger;
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly Func<string> _defaultUserAgentFn;

        /// <summary>
        /// Holds a dictionary of http clients by host.  Use GetHttpClient(host) to retrieve or create a client for web requests.
        /// DON'T dispose it after use.
        /// </summary>
        /// <value>The HTTP clients.</value>
        private readonly ConcurrentDictionary<string, HttpClient> _httpClients = new ConcurrentDictionary<string, HttpClient>();

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientManager" /> class.
        /// </summary>
        public HttpClientManager(
            IApplicationPaths appPaths,
            ILogger<HttpClientManager> logger,
            IFileSystem fileSystem,
            Func<string> defaultUserAgentFn)
        {
            if (appPaths == null)
            {
                throw new ArgumentNullException(nameof(appPaths));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger;
            _fileSystem = fileSystem;
            _appPaths = appPaths;
            _defaultUserAgentFn = defaultUserAgentFn;
        }

        /// <summary>
        /// Gets the correct http client for the given url.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <returns>HttpClient.</returns>
        private HttpClient GetHttpClient(string url)
        {
            var key = GetHostFromUrl(url);

            if (!_httpClients.TryGetValue(key, out var client))
            {
                client = new HttpClient()
                {
                    BaseAddress = new Uri(url)
                };

                _httpClients.TryAdd(key, client);
            }

            return client;
        }

        private HttpRequestMessage GetRequestMessage(HttpRequestOptions options, HttpMethod method)
        {
            string url = options.Url;
            var uriAddress = new Uri(url);
            string userInfo = uriAddress.UserInfo;
            if (!string.IsNullOrWhiteSpace(userInfo))
            {
                _logger.LogWarning("Found userInfo in url: {0} ... url: {1}", userInfo, url);
                url = url.Replace(userInfo + '@', string.Empty);
            }

            var request = new HttpRequestMessage(method, url);

            AddRequestHeaders(request, options);

            switch (options.DecompressionMethod)
            {
                case CompressionMethod.Deflate | CompressionMethod.Gzip:
                    request.Headers.Add(HeaderNames.AcceptEncoding, new[] { "gzip", "deflate" });
                    break;
                case CompressionMethod.Deflate:
                    request.Headers.Add(HeaderNames.AcceptEncoding, "deflate");
                    break;
                case CompressionMethod.Gzip:
                    request.Headers.Add(HeaderNames.AcceptEncoding, "gzip");
                    break;
                default:
                    break;
            }

            if (options.EnableKeepAlive)
            {
                request.Headers.Add(HeaderNames.Connection, "Keep-Alive");
            }

            //request.Headers.Add(HeaderNames.CacheControl, "no-cache");

            /*
            if (!string.IsNullOrWhiteSpace(userInfo))
            {
                var parts = userInfo.Split(':');
                if (parts.Length == 2)
                {
                    request.Headers.Add(HeaderNames., GetCredential(url, parts[0], parts[1]);
                }
            }
            */

            return request;
        }

        private void AddRequestHeaders(HttpRequestMessage request, HttpRequestOptions options)
        {
            var hasUserAgent = false;

            foreach (var header in options.RequestHeaders)
            {
                if (string.Equals(header.Key, HeaderNames.UserAgent, StringComparison.OrdinalIgnoreCase))
                {
                    hasUserAgent = true;
                }

                request.Headers.Add(header.Key, header.Value);
            }

            if (!hasUserAgent && options.EnableDefaultUserAgent)
            {
                request.Headers.Add(HeaderNames.UserAgent, _defaultUserAgentFn());
            }
        }

        /// <summary>
        /// Gets the response internal.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>Task{HttpResponseInfo}.</returns>
        public Task<HttpResponseInfo> GetResponse(HttpRequestOptions options)
            => SendAsync(options, HttpMethod.Get);

        /// <summary>
        /// Performs a GET request and returns the resulting stream
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>Task{Stream}.</returns>
        public async Task<Stream> Get(HttpRequestOptions options)
        {
            var response = await GetResponse(options).ConfigureAwait(false);
            return response.Content;
        }

        /// <summary>
        /// send as an asynchronous operation.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="httpMethod">The HTTP method.</param>
        /// <returns>Task{HttpResponseInfo}.</returns>
        public Task<HttpResponseInfo> SendAsync(HttpRequestOptions options, string httpMethod)
            => SendAsync(options, new HttpMethod(httpMethod));

        /// <summary>
        /// send as an asynchronous operation.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="httpMethod">The HTTP method.</param>
        /// <returns>Task{HttpResponseInfo}.</returns>
        public async Task<HttpResponseInfo> SendAsync(HttpRequestOptions options, HttpMethod httpMethod)
        {
            if (options.CacheMode == CacheMode.None)
            {
                return await SendAsyncInternal(options, httpMethod).ConfigureAwait(false);
            }

            var url = options.Url;
            var urlHash = url.ToLowerInvariant().GetMD5().ToString("N");

            var responseCachePath = Path.Combine(_appPaths.CachePath, "httpclient", urlHash);

            var response = GetCachedResponse(responseCachePath, options.CacheLength, url);
            if (response != null)
            {
                return response;
            }

            response = await SendAsyncInternal(options, httpMethod).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                await CacheResponse(response, responseCachePath).ConfigureAwait(false);
            }

            return response;
        }

        private HttpResponseInfo GetCachedResponse(string responseCachePath, TimeSpan cacheLength, string url)
        {
            if (File.Exists(responseCachePath)
                && _fileSystem.GetLastWriteTimeUtc(responseCachePath).Add(cacheLength) > DateTime.UtcNow)
            {
                var stream = _fileSystem.GetFileStream(responseCachePath, FileOpenMode.Open, FileAccessMode.Read, FileShareMode.Read, true);

                return new HttpResponseInfo
                {
                    ResponseUrl = url,
                    Content = stream,
                    StatusCode = HttpStatusCode.OK,
                    ContentLength = stream.Length
                };
            }

            return null;
        }

        private async Task CacheResponse(HttpResponseInfo response, string responseCachePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(responseCachePath));

            using (var fileStream = _fileSystem.GetFileStream(responseCachePath, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.None, true))
            {
                await response.Content.CopyToAsync(fileStream).ConfigureAwait(false);

                response.Content.Position = 0;
            }
        }

        private async Task<HttpResponseInfo> SendAsyncInternal(HttpRequestOptions options, HttpMethod httpMethod)
        {
            ValidateParams(options);

            options.CancellationToken.ThrowIfCancellationRequested();

            var client = GetHttpClient(options.Url);

            var httpWebRequest = GetRequestMessage(options, httpMethod);

            if (options.RequestContentBytes != null
                || !string.IsNullOrEmpty(options.RequestContent)
                || httpMethod == HttpMethod.Post)
            {
                if (options.RequestContentBytes != null)
                {
                    httpWebRequest.Content = new ByteArrayContent(options.RequestContentBytes);
                }
                else if (options.RequestContent != null)
                {
                    httpWebRequest.Content = new StringContent(
                        options.RequestContent,
                        null,
                        options.RequestContentType);
                }
                else
                {
                    httpWebRequest.Content = new ByteArrayContent(Array.Empty<byte>());
                }
            }

            if (options.LogRequest)
            {
                _logger.LogDebug("HttpClientManager {0}: {1}", httpMethod.ToString(), options.Url);
            }

            options.CancellationToken.ThrowIfCancellationRequested();

            if (!options.BufferContent)
            {
                var response = await client.SendAsync(httpWebRequest, HttpCompletionOption.ResponseHeadersRead, options.CancellationToken).ConfigureAwait(false);

                await EnsureSuccessStatusCode(response, options).ConfigureAwait(false);

                options.CancellationToken.ThrowIfCancellationRequested();

                var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                return new HttpResponseInfo(response.Headers, response.Content.Headers)
                {
                    Content = stream,
                    StatusCode = response.StatusCode,
                    ContentType = response.Content.Headers.ContentType?.MediaType,
                    ContentLength = response.Content.Headers.ContentLength,
                    ResponseUrl = response.Content.Headers.ContentLocation?.ToString()
                };
            }

            using (var response = await client.SendAsync(httpWebRequest, HttpCompletionOption.ResponseHeadersRead, options.CancellationToken).ConfigureAwait(false))
            {
                await EnsureSuccessStatusCode(response, options).ConfigureAwait(false);

                options.CancellationToken.ThrowIfCancellationRequested();

                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream, StreamDefaults.DefaultCopyToBufferSize, options.CancellationToken).ConfigureAwait(false);
                    memoryStream.Position = 0;

                    return new HttpResponseInfo(response.Headers, response.Content.Headers)
                    {
                        Content = memoryStream,
                        StatusCode = response.StatusCode,
                        ContentType = response.Content.Headers.ContentType?.MediaType,
                        ContentLength = memoryStream.Length,
                        ResponseUrl = response.Content.Headers.ContentLocation?.ToString()
                    };
                }
            }
        }

        public Task<HttpResponseInfo> Post(HttpRequestOptions options)
            => SendAsync(options, HttpMethod.Post);

        /// <summary>
        /// Downloads the contents of a given url into a temporary location
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>Task{System.String}.</returns>
        public async Task<string> GetTempFile(HttpRequestOptions options)
        {
            var response = await GetTempFileResponse(options).ConfigureAwait(false);
            return response.TempFilePath;
        }

        public async Task<HttpResponseInfo> GetTempFileResponse(HttpRequestOptions options)
        {
            ValidateParams(options);

            Directory.CreateDirectory(_appPaths.TempDirectory);

            var tempFile = Path.Combine(_appPaths.TempDirectory, Guid.NewGuid() + ".tmp");

            if (options.Progress == null)
            {
                throw new ArgumentException("Options did not have a Progress value.", nameof(options));
            }

            options.CancellationToken.ThrowIfCancellationRequested();

            var httpWebRequest = GetRequestMessage(options, HttpMethod.Get);

            options.Progress.Report(0);

            if (options.LogRequest)
            {
                _logger.LogDebug("HttpClientManager.GetTempFileResponse url: {0}", options.Url);
            }

            var client = GetHttpClient(options.Url);

            try
            {
                options.CancellationToken.ThrowIfCancellationRequested();

                using (var response = (await client.SendAsync(httpWebRequest, options.CancellationToken).ConfigureAwait(false)))
                {
                    await EnsureSuccessStatusCode(response, options).ConfigureAwait(false);

                    options.CancellationToken.ThrowIfCancellationRequested();

                    using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var fs = _fileSystem.GetFileStream(tempFile, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.Read, true))
                    {
                        await stream.CopyToAsync(fs, StreamDefaults.DefaultCopyToBufferSize, options.CancellationToken).ConfigureAwait(false);
                    }

                    options.Progress.Report(100);

                    var responseInfo = new HttpResponseInfo(response.Headers, response.Content.Headers)
                    {
                        TempFilePath = tempFile,
                        StatusCode = response.StatusCode,
                        ContentType = response.Content.Headers.ContentType?.MediaType,
                        ContentLength = response.Content.Headers.ContentLength
                    };

                    return responseInfo;
                }
            }
            catch (Exception ex)
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }

                throw GetException(ex, options);
            }
        }

        private Exception GetException(Exception ex, HttpRequestOptions options)
        {
            if (ex is HttpException)
            {
                return ex;
            }

            var webException = ex as WebException
                               ?? ex.InnerException as WebException;

            if (webException != null)
            {
                if (options.LogErrors)
                {
                    _logger.LogError(webException, "Error {Status} getting response from {Url}", webException.Status, options.Url);
                }

                var exception = new HttpException(webException.Message, webException);

                using (var response = webException.Response as HttpWebResponse)
                {
                    if (response != null)
                    {
                        exception.StatusCode = response.StatusCode;
                    }
                }

                if (!exception.StatusCode.HasValue)
                {
                    if (webException.Status == WebExceptionStatus.NameResolutionFailure ||
                        webException.Status == WebExceptionStatus.ConnectFailure)
                    {
                        exception.IsTimedOut = true;
                    }
                }

                return exception;
            }

            var operationCanceledException = ex as OperationCanceledException
                                             ?? ex.InnerException as OperationCanceledException;

            if (operationCanceledException != null)
            {
                return GetCancellationException(options, options.CancellationToken, operationCanceledException);
            }

            if (options.LogErrors)
            {
                _logger.LogError(ex, "Error getting response from {Url}", options.Url);
            }

            return ex;
        }

        private void ValidateParams(HttpRequestOptions options)
        {
            if (string.IsNullOrEmpty(options.Url))
            {
                throw new ArgumentNullException(nameof(options));
            }
        }

        /// <summary>
        /// Gets the host from URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>System.String.</returns>
        private static string GetHostFromUrl(string url)
        {
            var index = url.IndexOf("://", StringComparison.OrdinalIgnoreCase);

            if (index != -1)
            {
                url = url.Substring(index + 3);
                var host = url.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(host))
                {
                    return host;
                }
            }

            return url;
        }

        /// <summary>
        /// Throws the cancellation exception.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="exception">The exception.</param>
        /// <returns>Exception.</returns>
        private Exception GetCancellationException(HttpRequestOptions options, CancellationToken cancellationToken, OperationCanceledException exception)
        {
            // If the HttpClient's timeout is reached, it will cancel the Task internally
            if (!cancellationToken.IsCancellationRequested)
            {
                var msg = string.Format("Connection to {0} timed out", options.Url);

                if (options.LogErrors)
                {
                    _logger.LogError(msg);
                }

                // Throw an HttpException so that the caller doesn't think it was cancelled by user code
                return new HttpException(msg, exception)
                {
                    IsTimedOut = true
                };
            }

            return exception;
        }

        private async Task EnsureSuccessStatusCode(HttpResponseMessage response, HttpRequestOptions options)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var msg = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            _logger.LogError("HTTP request failed with message: {Message}", msg);

            throw new HttpException(response.ReasonPhrase)
            {
                StatusCode = response.StatusCode
            };
        }
    }
}
