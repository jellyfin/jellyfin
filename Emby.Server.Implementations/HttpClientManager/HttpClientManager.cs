using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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
        /// <summary>
        /// When one request to a host times out, we'll ban all other requests for this period of time, to prevent scans from stalling
        /// </summary>
        private const int TimeoutSeconds = 30;

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The _app paths
        /// </summary>
        private readonly IApplicationPaths _appPaths;

        private readonly IFileSystem _fileSystem;
        private readonly Func<string> _defaultUserAgentFn;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientManager" /> class.
        /// </summary>
        public HttpClientManager(
            IApplicationPaths appPaths,
            ILoggerFactory loggerFactory,
            IFileSystem fileSystem,
            Func<string> defaultUserAgentFn)
        {
            if (appPaths == null)
            {
                throw new ArgumentNullException(nameof(appPaths));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger(nameof(HttpClientManager));
            _fileSystem = fileSystem;
            _appPaths = appPaths;
            _defaultUserAgentFn = defaultUserAgentFn;

            // http://stackoverflow.com/questions/566437/http-post-returns-the-error-417-expectation-failed-c
            ServicePointManager.Expect100Continue = false;
        }

        /// <summary>
        /// Holds a dictionary of http clients by host.  Use GetHttpClient(host) to retrieve or create a client for web requests.
        /// DON'T dispose it after use.
        /// </summary>
        /// <value>The HTTP clients.</value>
        private readonly ConcurrentDictionary<string, HttpClient> _httpClients = new ConcurrentDictionary<string, HttpClient>();

        /// <summary>
        /// Gets
        /// </summary>
        /// <param name="url">The host.</param>
        /// <param name="enableHttpCompression">if set to <c>true</c> [enable HTTP compression].</param>
        /// <returns>HttpClient.</returns>
        /// <exception cref="ArgumentNullException">host</exception>
        private HttpClient GetHttpClient(string url, bool enableHttpCompression)
        {
            var key = GetHostFromUrl(url) + enableHttpCompression;

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
                url = url.Replace(userInfo + "@", string.Empty);
            }

            var request = new HttpRequestMessage(method, url);

            AddRequestHeaders(request, options);

            if (options.EnableHttpCompression)
            {
                if (options.DecompressionMethod.HasValue
                    && options.DecompressionMethod.Value == CompressionMethod.Gzip)
                {
                    request.Headers.Add(HeaderNames.AcceptEncoding, new[] { "gzip", "deflate" });
                }
                else
                {
                    request.Headers.Add(HeaderNames.AcceptEncoding, "deflate");
                }
            }

            if (options.EnableKeepAlive)
            {
                request.Headers.Add(HeaderNames.Connection, "Keep-Alive");
            }

            if (!string.IsNullOrEmpty(options.Host))
            {
                request.Headers.Add(HeaderNames.Host, options.Host);
            }

            if (!string.IsNullOrEmpty(options.Referer))
            {
                request.Headers.Add(HeaderNames.Referer, options.Referer);
            }

            //request.Headers.Add(HeaderNames.CacheControl, "no-cache");

            //request.Headers.Add(HeaderNames., options.TimeoutMs;

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
        {
            return SendAsync(options, HttpMethod.Get);
        }

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
        /// <exception cref="HttpException">
        /// </exception>
        public Task<HttpResponseInfo> SendAsync(HttpRequestOptions options, string httpMethod)
        {
            var httpMethod2 = GetHttpMethod(httpMethod);
            return SendAsync(options, httpMethod2);
        }

        /// <summary>
        /// send as an asynchronous operation.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="httpMethod">The HTTP method.</param>
        /// <returns>Task{HttpResponseInfo}.</returns>
        /// <exception cref="HttpException">
        /// </exception>
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

        private HttpMethod GetHttpMethod(string httpMethod)
        {
            if (httpMethod.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                return HttpMethod.Delete;
            }
            else if (httpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                return HttpMethod.Get;
            }
            else if (httpMethod.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
            {
                return HttpMethod.Head;
            }
            else if (httpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                return HttpMethod.Options;
            }
            else if (httpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                return HttpMethod.Post;
            }
            else if (httpMethod.Equals("PUT", StringComparison.OrdinalIgnoreCase))
            {
                return HttpMethod.Put;
            }
            else if (httpMethod.Equals("TRACE", StringComparison.OrdinalIgnoreCase))
            {
                return HttpMethod.Trace;
            }

            throw new ArgumentException("Invalid HTTP method", nameof(httpMethod));
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

            var client = GetHttpClient(options.Url, options.EnableHttpCompression);

            var httpWebRequest = GetRequestMessage(options, httpMethod);

            if (options.RequestContentBytes != null ||
                !string.IsNullOrEmpty(options.RequestContent) ||
                httpMethod == HttpMethod.Post)
            {
                try
                {
                    httpWebRequest.Content = new StringContent(Encoding.UTF8.GetString(options.RequestContentBytes) ?? options.RequestContent ?? string.Empty);

                    var contentType = options.RequestContentType ?? "application/x-www-form-urlencoded";

                    if (options.AppendCharsetToMimeType)
                    {
                        contentType = contentType.TrimEnd(';') + "; charset=\"utf-8\"";
                    }

                    httpWebRequest.Headers.Add(HeaderNames.ContentType, contentType);
                    await client.SendAsync(httpWebRequest).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new HttpException(ex.Message) { IsTimedOut = true };
                }
            }

            if (options.ResourcePool != null)
            {
                await options.ResourcePool.WaitAsync(options.CancellationToken).ConfigureAwait(false);
            }

            if (options.LogRequest)
            {
                _logger.LogDebug("HttpClientManager {0}: {1}", httpMethod.ToString(), options.Url);
            }

            try
            {
                options.CancellationToken.ThrowIfCancellationRequested();

                /*if (!options.BufferContent)
                {
                    var response = await client.HttpClient.SendAsync(httpWebRequest).ConfigureAwait(false);

                    await EnsureSuccessStatusCode(client, response, options).ConfigureAwait(false);

                    options.CancellationToken.ThrowIfCancellationRequested();

                    return GetResponseInfo(response, await response.Content.ReadAsStreamAsync().ConfigureAwait(false), response.Content.Headers.ContentLength, response);
                }*/

                using (var response = await client.SendAsync(httpWebRequest).ConfigureAwait(false))
                {
                    await EnsureSuccessStatusCode(response, options).ConfigureAwait(false);

                    options.CancellationToken.ThrowIfCancellationRequested();

                    using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        var memoryStream = new MemoryStream();
                        await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
                        memoryStream.Position = 0;

                        return GetResponseInfo(response, memoryStream, memoryStream.Length, null);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                throw GetCancellationException(options, options.CancellationToken, ex);
            }
            finally
            {
                options.ResourcePool?.Release();
            }
        }

        private HttpResponseInfo GetResponseInfo(HttpResponseMessage httpResponse, Stream content, long? contentLength, IDisposable disposable)
        {
            var responseInfo = new HttpResponseInfo(disposable)
            {
                Content = content,
                StatusCode = httpResponse.StatusCode,
                ContentType = httpResponse.Content.Headers.ContentType?.MediaType,
                ContentLength = contentLength,
                ResponseUrl = httpResponse.Content.Headers.ContentLocation?.ToString()
            };

            if (httpResponse.Headers != null)
            {
                SetHeaders(httpResponse.Content.Headers, responseInfo);
            }

            return responseInfo;
        }

        private HttpResponseInfo GetResponseInfo(HttpResponseMessage httpResponse, string tempFile, long? contentLength)
        {
            var responseInfo = new HttpResponseInfo
            {
                TempFilePath = tempFile,
                StatusCode = httpResponse.StatusCode,
                ContentType = httpResponse.Content.Headers.ContentType?.MediaType,
                ContentLength = contentLength
            };

            if (httpResponse.Headers != null)
            {
                SetHeaders(httpResponse.Content.Headers, responseInfo);
            }

            return responseInfo;
        }

        private static void SetHeaders(HttpContentHeaders headers, HttpResponseInfo responseInfo)
        {
            foreach (var key in headers)
            {
                responseInfo.Headers[key.Key] = string.Join(", ", key.Value);
            }
        }

        public Task<HttpResponseInfo> Post(HttpRequestOptions options)
        {
            return SendAsync(options, HttpMethod.Post);
        }

        /// <summary>
        /// Downloads the contents of a given url into a temporary location
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>Task{System.String}.</returns>
        public async Task<string> GetTempFile(HttpRequestOptions options)
        {
            using (var response = await GetTempFileResponse(options).ConfigureAwait(false))
            {
                return response.TempFilePath;
            }
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

            if (options.ResourcePool != null)
            {
                await options.ResourcePool.WaitAsync(options.CancellationToken).ConfigureAwait(false);
            }

            options.Progress.Report(0);

            if (options.LogRequest)
            {
                _logger.LogDebug("HttpClientManager.GetTempFileResponse url: {0}", options.Url);
            }

            var client = GetHttpClient(options.Url, options.EnableHttpCompression);

            try
            {
                options.CancellationToken.ThrowIfCancellationRequested();

                using (var response = (await client.SendAsync(httpWebRequest).ConfigureAwait(false)))
                {
                    await EnsureSuccessStatusCode(response, options).ConfigureAwait(false);

                    options.CancellationToken.ThrowIfCancellationRequested();

                    using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var fs = _fileSystem.GetFileStream(tempFile, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.Read, true))
                    {
                        await stream.CopyToAsync(fs, StreamDefaults.DefaultCopyToBufferSize, options.CancellationToken).ConfigureAwait(false);
                    }

                    options.Progress.Report(100);

                    var contentLength = response.Content.Headers.ContentLength;
                    return GetResponseInfo(response, tempFile, contentLength);
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
            finally
            {
                options.ResourcePool?.Release();
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
                    _logger.LogError(webException, "Error {status} getting response from {url}", webException.Status, options.Url);
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
                _logger.LogError(ex, "Error getting response from {url}", options.Url);
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
            _logger.LogError(msg);

            throw new HttpException(response.ReasonPhrase)
            {
                StatusCode = response.StatusCode
            };
        }
    }
}
