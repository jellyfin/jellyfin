using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using MediaBrowser.Common;
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
    /// Class HttpClientManager.
    /// </summary>
    public class HttpClientManager : IHttpClient
    {
        private readonly ILogger _logger;
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly IApplicationHost _appHost;

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
            IApplicationHost appHost)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystem = fileSystem;
            _appPaths = appPaths ?? throw new ArgumentNullException(nameof(appPaths));
            _appHost = appHost;
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
                url = url.Replace(userInfo + '@', string.Empty, StringComparison.Ordinal);
            }

            var request = new HttpRequestMessage(method, url);

            foreach (var header in options.RequestHeaders)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (options.EnableDefaultUserAgent
                && !request.Headers.TryGetValues(HeaderNames.UserAgent, out _))
            {
                request.Headers.Add(HeaderNames.UserAgent, _appHost.ApplicationUserAgent);
            }

            switch (options.DecompressionMethod)
            {
                case CompressionMethods.Deflate | CompressionMethods.Gzip:
                    request.Headers.Add(HeaderNames.AcceptEncoding, new[] { "gzip", "deflate" });
                    break;
                case CompressionMethods.Deflate:
                    request.Headers.Add(HeaderNames.AcceptEncoding, "deflate");
                    break;
                case CompressionMethods.Gzip:
                    request.Headers.Add(HeaderNames.AcceptEncoding, "gzip");
                    break;
                default:
                    break;
            }

            if (options.EnableKeepAlive)
            {
                request.Headers.Add(HeaderNames.Connection, "Keep-Alive");
            }

            // request.Headers.Add(HeaderNames.CacheControl, "no-cache");

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
            var urlHash = url.ToUpperInvariant().GetMD5().ToString("N", CultureInfo.InvariantCulture);

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
                var stream = new FileStream(responseCachePath, FileMode.Open, FileAccess.Read, FileShare.Read, IODefaults.FileStreamBufferSize, true);

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

            using (var fileStream = new FileStream(
                responseCachePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                IODefaults.FileStreamBufferSize,
                true))
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

            if (!string.IsNullOrEmpty(options.RequestContent)
                || httpMethod == HttpMethod.Post)
            {
                if (options.RequestContent != null)
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

            options.CancellationToken.ThrowIfCancellationRequested();

            var response = await client.SendAsync(
                httpWebRequest,
                options.BufferContent || options.CacheMode == CacheMode.Unconditional ? HttpCompletionOption.ResponseContentRead : HttpCompletionOption.ResponseHeadersRead,
                options.CancellationToken).ConfigureAwait(false);

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

        /// <inheritdoc />
        public Task<HttpResponseInfo> Post(HttpRequestOptions options)
            => SendAsync(options, HttpMethod.Post);

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

        private async Task EnsureSuccessStatusCode(HttpResponseMessage response, HttpRequestOptions options)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            if (options.LogErrorResponseBody)
            {
                string msg = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogError("HTTP request failed with message: {Message}", msg);
            }

            throw new HttpException(response.ReasonPhrase)
            {
                StatusCode = response.StatusCode
            };
        }
    }
}
