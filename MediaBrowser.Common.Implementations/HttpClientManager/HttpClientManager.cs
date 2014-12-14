using System.Net.Sockets;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Implementations.HttpClientManager
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
        private readonly IConfigurationManager _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientManager" /> class.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <exception cref="System.ArgumentNullException">appPaths
        /// or
        /// logger</exception>
        public HttpClientManager(IApplicationPaths appPaths, ILogger logger, IFileSystem fileSystem, IConfigurationManager config)
        {
            if (appPaths == null)
            {
                throw new ArgumentNullException("appPaths");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _logger = logger;
            _fileSystem = fileSystem;
            _config = config;
            _appPaths = appPaths;

            // http://stackoverflow.com/questions/566437/http-post-returns-the-error-417-expectation-failed-c
            ServicePointManager.Expect100Continue = false;
        }

        /// <summary>
        /// Holds a dictionary of http clients by host.  Use GetHttpClient(host) to retrieve or create a client for web requests.
        /// DON'T dispose it after use.
        /// </summary>
        /// <value>The HTTP clients.</value>
        private readonly ConcurrentDictionary<string, HttpClientInfo> _httpClients = new ConcurrentDictionary<string, HttpClientInfo>();

        /// <summary>
        /// Gets
        /// </summary>
        /// <param name="host">The host.</param>
        /// <param name="enableHttpCompression">if set to <c>true</c> [enable HTTP compression].</param>
        /// <returns>HttpClient.</returns>
        /// <exception cref="System.ArgumentNullException">host</exception>
        private HttpClientInfo GetHttpClient(string host, bool enableHttpCompression)
        {
            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentNullException("host");
            }

            HttpClientInfo client;

            var key = host + enableHttpCompression;

            if (!_httpClients.TryGetValue(key, out client))
            {
                client = new HttpClientInfo();

                _httpClients.TryAdd(key, client);
            }

            return client;
        }

        private WebRequest GetRequest(HttpRequestOptions options, string method, bool enableHttpCompression)
        {
            var request = (HttpWebRequest)WebRequest.Create(options.Url);

            AddRequestHeaders(request, options);

            request.AutomaticDecompression = enableHttpCompression ? DecompressionMethods.Deflate : DecompressionMethods.None;

            request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);

            if (options.EnableKeepAlive)
            {
                request.KeepAlive = true;
            }

            request.Method = method;
            request.Timeout = options.TimeoutMs;

            if (!string.IsNullOrEmpty(options.Host))
            {
                request.Host = options.Host;
            }

            if (!string.IsNullOrEmpty(options.Referer))
            {
                request.Referer = options.Referer;
            }

            //request.ServicePoint.BindIPEndPointDelegate = BindIPEndPointCallback;

            return request;
        }

        private static IPEndPoint BindIPEndPointCallback(ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount)
        {
            // Prefer local ipv4
            if (remoteEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return new IPEndPoint(IPAddress.IPv6Any, 0);
            }

            return new IPEndPoint(IPAddress.Any, 0);
        }

        private void AddRequestHeaders(HttpWebRequest request, HttpRequestOptions options)
        {
            foreach (var header in options.RequestHeaders.ToList())
            {
                if (string.Equals(header.Key, "Accept", StringComparison.OrdinalIgnoreCase))
                {
                    request.Accept = header.Value;
                }
                else if (string.Equals(header.Key, "User-Agent", StringComparison.OrdinalIgnoreCase))
                {
                    request.UserAgent = header.Value;
                }
                else
                {
                    request.Headers.Set(header.Key, header.Value);
                }
            }
        }

        /// <summary>
        /// The _semaphoreLocks
        /// </summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphoreLocks = new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Gets the lock.
        /// </summary>
        /// <param name="url">The filename.</param>
        /// <returns>System.Object.</returns>
        private SemaphoreSlim GetLock(string url)
        {
            return _semaphoreLocks.GetOrAdd(url, key => new SemaphoreSlim(1, 1));
        }

        /// <summary>
        /// Gets the response internal.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>Task{HttpResponseInfo}.</returns>
        public Task<HttpResponseInfo> GetResponse(HttpRequestOptions options)
        {
            return SendAsync(options, "GET");
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
        /// Performs a GET request and returns the resulting stream
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="resourcePool">The resource pool.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        public Task<Stream> Get(string url, SemaphoreSlim resourcePool, CancellationToken cancellationToken)
        {
            return Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = resourcePool,
                CancellationToken = cancellationToken,
            });
        }

        /// <summary>
        /// Gets the specified URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        public Task<Stream> Get(string url, CancellationToken cancellationToken)
        {
            return Get(url, null, cancellationToken);
        }

        /// <summary>
        /// send as an asynchronous operation.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="httpMethod">The HTTP method.</param>
        /// <returns>Task{HttpResponseInfo}.</returns>
        /// <exception cref="HttpException">
        /// </exception>
        public async Task<HttpResponseInfo> SendAsync(HttpRequestOptions options, string httpMethod)
        {
            HttpResponseInfo response;

            if (options.CacheMode == CacheMode.None)
            {
                response = await SendAsyncInternal(options, httpMethod).ConfigureAwait(false);
                return response;
            }

            var url = options.Url;
            var urlHash = url.ToLower().GetMD5().ToString("N");
            var semaphore = GetLock(url);

            var responseCachePath = Path.Combine(_appPaths.CachePath, "httpclient", urlHash);

            response = await GetCachedResponse(responseCachePath, options.CacheLength, url).ConfigureAwait(false);
            if (response != null)
            {
                return response;
            }

            await semaphore.WaitAsync(options.CancellationToken).ConfigureAwait(false);

            try
            {
                response = await GetCachedResponse(responseCachePath, options.CacheLength, url).ConfigureAwait(false);
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
            finally
            {
                semaphore.Release();
            }
        }

        private async Task<HttpResponseInfo> GetCachedResponse(string responseCachePath, TimeSpan cacheLength, string url)
        {
            try
            {
                if (_fileSystem.GetLastWriteTimeUtc(responseCachePath).Add(cacheLength) > DateTime.UtcNow)
                {
                    using (var stream = _fileSystem.GetFileStream(responseCachePath, FileMode.Open, FileAccess.Read, FileShare.Read, true))
                    {
                        var memoryStream = new MemoryStream();

                        await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
                        memoryStream.Position = 0;

                        return new HttpResponseInfo
                        {
                            ResponseUrl = url,
                            Content = memoryStream,
                            StatusCode = HttpStatusCode.OK,
                            Headers = new NameValueCollection(),
                            ContentLength = memoryStream.Length
                        };
                    }
                }
            }
            catch (FileNotFoundException)
            {

            }
            catch (DirectoryNotFoundException)
            {

            }

            return null;
        }

        private async Task CacheResponse(HttpResponseInfo response, string responseCachePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(responseCachePath));

            using (var responseStream = response.Content)
            {
                using (var fileStream = _fileSystem.GetFileStream(responseCachePath, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                {
                    var memoryStream = new MemoryStream();

                    await responseStream.CopyToAsync(memoryStream).ConfigureAwait(false);

                    memoryStream.Position = 0;
                    await memoryStream.CopyToAsync(fileStream).ConfigureAwait(false);

                    memoryStream.Position = 0;
                    response.Content = memoryStream;
                }
            }
        }

        private async Task<HttpResponseInfo> SendAsyncInternal(HttpRequestOptions options, string httpMethod)
        {
            ValidateParams(options);

            options.CancellationToken.ThrowIfCancellationRequested();

            var client = GetHttpClient(GetHostFromUrl(options.Url), options.EnableHttpCompression);

            if ((DateTime.UtcNow - client.LastTimeout).TotalSeconds < TimeoutSeconds)
            {
                throw new HttpException(string.Format("Cancelling connection to {0} due to a previous timeout.", options.Url))
                {
                    IsTimedOut = true
                };
            }

            var httpWebRequest = GetRequest(options, httpMethod, options.EnableHttpCompression);

            if (options.RequestContentBytes != null ||
                !string.IsNullOrEmpty(options.RequestContent) ||
                string.Equals(httpMethod, "post", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = options.RequestContentBytes ??
                    Encoding.UTF8.GetBytes(options.RequestContent ?? string.Empty);

                httpWebRequest.ContentType = options.RequestContentType ?? "application/x-www-form-urlencoded";

                httpWebRequest.ContentLength = bytes.Length;
                httpWebRequest.GetRequestStream().Write(bytes, 0, bytes.Length);
            }

            if (options.ResourcePool != null)
            {
                await options.ResourcePool.WaitAsync(options.CancellationToken).ConfigureAwait(false);
            }

            if ((DateTime.UtcNow - client.LastTimeout).TotalSeconds < TimeoutSeconds)
            {
                if (options.ResourcePool != null)
                {
                    options.ResourcePool.Release();
                }

                throw new HttpException(string.Format("Connection to {0} timed out", options.Url)) { IsTimedOut = true };
            }

            if (options.LogRequest)
            {
                _logger.Info("HttpClientManager {0}: {1}", httpMethod.ToUpper(), options.Url);
            }

            try
            {
                options.CancellationToken.ThrowIfCancellationRequested();

                if (!options.BufferContent)
                {
                    var response = await GetResponseAsync(httpWebRequest, TimeSpan.FromMilliseconds(options.TimeoutMs)).ConfigureAwait(false);

                    var httpResponse = (HttpWebResponse)response;

                    EnsureSuccessStatusCode(httpResponse, options);

                    options.CancellationToken.ThrowIfCancellationRequested();

                    return GetResponseInfo(httpResponse, httpResponse.GetResponseStream(), GetContentLength(httpResponse), httpResponse);
                }

                using (var response = await GetResponseAsync(httpWebRequest, TimeSpan.FromMilliseconds(options.TimeoutMs)).ConfigureAwait(false))
                {
                    var httpResponse = (HttpWebResponse)response;

                    EnsureSuccessStatusCode(httpResponse, options);

                    options.CancellationToken.ThrowIfCancellationRequested();

                    using (var stream = httpResponse.GetResponseStream())
                    {
                        var memoryStream = new MemoryStream();

                        await stream.CopyToAsync(memoryStream).ConfigureAwait(false);

                        memoryStream.Position = 0;

                        return GetResponseInfo(httpResponse, memoryStream, memoryStream.Length, null);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                var exception = GetCancellationException(options.Url, options.CancellationToken, ex);

                var httpException = exception as HttpException;

                if (httpException != null && httpException.IsTimedOut)
                {
                    client.LastTimeout = DateTime.UtcNow;
                }

                throw exception;
            }
            catch (Exception ex)
            {
                throw GetException(ex, options);
            }
            finally
            {
                if (options.ResourcePool != null)
                {
                    options.ResourcePool.Release();
                }
            }
        }

        /// <summary>
        /// Gets the exception.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <param name="options">The options.</param>
        /// <returns>HttpException.</returns>
        private HttpException GetException(WebException ex, HttpRequestOptions options)
        {
            _logger.ErrorException("Error getting response from " + options.Url, ex);

            var exception = new HttpException(ex.Message, ex);

            var response = ex.Response as HttpWebResponse;
            if (response != null)
            {
                exception.StatusCode = response.StatusCode;
            }

            return exception;
        }

        private HttpResponseInfo GetResponseInfo(HttpWebResponse httpResponse, Stream content, long? contentLength, IDisposable disposable)
        {
            return new HttpResponseInfo(disposable)
            {
                Content = content,

                StatusCode = httpResponse.StatusCode,

                ContentType = httpResponse.ContentType,

                Headers = new NameValueCollection(httpResponse.Headers),

                ContentLength = contentLength,

                ResponseUrl = httpResponse.ResponseUri.ToString()
            };
        }

        private HttpResponseInfo GetResponseInfo(HttpWebResponse httpResponse, string tempFile, long? contentLength)
        {
            return new HttpResponseInfo
            {
                TempFilePath = tempFile,

                StatusCode = httpResponse.StatusCode,

                ContentType = httpResponse.ContentType,

                Headers = httpResponse.Headers,

                ContentLength = contentLength
            };
        }

        public Task<HttpResponseInfo> Post(HttpRequestOptions options)
        {
            return SendAsync(options, "POST");
        }

        /// <summary>
        /// Performs a POST request
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="postData">Params to add to the POST data.</param>
        /// <returns>stream on success, null on failure</returns>
        public async Task<Stream> Post(HttpRequestOptions options, Dictionary<string, string> postData)
        {
            options.SetPostData(postData);

            var response = await Post(options).ConfigureAwait(false);

            return response.Content;
        }

        /// <summary>
        /// Performs a POST request
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="postData">Params to add to the POST data.</param>
        /// <param name="resourcePool">The resource pool.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>stream on success, null on failure</returns>
        public Task<Stream> Post(string url, Dictionary<string, string> postData, SemaphoreSlim resourcePool, CancellationToken cancellationToken)
        {
            return Post(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = resourcePool,
                CancellationToken = cancellationToken

            }, postData);
        }

        /// <summary>
        /// Downloads the contents of a given url into a temporary location
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>Task{System.String}.</returns>
        /// <exception cref="System.ArgumentNullException">progress</exception>
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
                throw new ArgumentNullException("progress");
            }

            options.CancellationToken.ThrowIfCancellationRequested();

            var httpWebRequest = GetRequest(options, "GET", options.EnableHttpCompression);

            if (options.ResourcePool != null)
            {
                await options.ResourcePool.WaitAsync(options.CancellationToken).ConfigureAwait(false);
            }

            options.Progress.Report(0);

            if (options.LogRequest)
            {
                _logger.Info("HttpClientManager.GetTempFileResponse url: {0}", options.Url);
            }

            try
            {
                options.CancellationToken.ThrowIfCancellationRequested();

                using (var response = await httpWebRequest.GetResponseAsync().ConfigureAwait(false))
                {
                    var httpResponse = (HttpWebResponse)response;

                    EnsureSuccessStatusCode(httpResponse, options);

                    options.CancellationToken.ThrowIfCancellationRequested();

                    var contentLength = GetContentLength(httpResponse);

                    if (!contentLength.HasValue)
                    {
                        // We're not able to track progress
                        using (var stream = httpResponse.GetResponseStream())
                        {
                            using (var fs = _fileSystem.GetFileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                            {
                                await stream.CopyToAsync(fs, StreamDefaults.DefaultCopyToBufferSize, options.CancellationToken).ConfigureAwait(false);
                            }
                        }
                    }
                    else
                    {
                        using (var stream = ProgressStream.CreateReadProgressStream(httpResponse.GetResponseStream(), options.Progress.Report, contentLength.Value))
                        {
                            using (var fs = _fileSystem.GetFileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                            {
                                await stream.CopyToAsync(fs, StreamDefaults.DefaultCopyToBufferSize, options.CancellationToken).ConfigureAwait(false);
                            }
                        }
                    }

                    options.Progress.Report(100);

                    return GetResponseInfo(httpResponse, tempFile, contentLength);
                }
            }
            catch (Exception ex)
            {
                DeleteTempFile(tempFile);
                throw GetException(ex, options);
            }
            finally
            {
                if (options.ResourcePool != null)
                {
                    options.ResourcePool.Release();
                }
            }
        }

        private long? GetContentLength(HttpWebResponse response)
        {
            var length = response.ContentLength;

            if (length == 0)
            {
                return null;
            }

            return length;
        }

        protected static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        private Exception GetException(Exception ex, HttpRequestOptions options)
        {
            var webException = ex as WebException
                ?? ex.InnerException as WebException;

            if (webException != null)
            {
                return GetException(webException, options);
            }

            var operationCanceledException = ex as OperationCanceledException
                ?? ex.InnerException as OperationCanceledException;

            if (operationCanceledException != null)
            {
                return GetCancellationException(options.Url, options.CancellationToken, operationCanceledException);
            }

            _logger.ErrorException("Error getting response from " + options.Url, ex);

            return ex;
        }

        private void DeleteTempFile(string file)
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
                // Might not have been created at all. No need to worry.
            }
        }

        private void ValidateParams(HttpRequestOptions options)
        {
            if (string.IsNullOrEmpty(options.Url))
            {
                throw new ArgumentNullException("options");
            }
        }

        /// <summary>
        /// Gets the host from URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>System.String.</returns>
        private string GetHostFromUrl(string url)
        {
            var start = url.IndexOf("://", StringComparison.OrdinalIgnoreCase) + 3;
            var len = url.IndexOf('/', start) - start;
            return url.Substring(start, len);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                _httpClients.Clear();
            }
        }

        /// <summary>
        /// Throws the cancellation exception.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="exception">The exception.</param>
        /// <returns>Exception.</returns>
        private Exception GetCancellationException(string url, CancellationToken cancellationToken, OperationCanceledException exception)
        {
            // If the HttpClient's timeout is reached, it will cancel the Task internally
            if (!cancellationToken.IsCancellationRequested)
            {
                var msg = string.Format("Connection to {0} timed out", url);

                _logger.Error(msg);

                // Throw an HttpException so that the caller doesn't think it was cancelled by user code
                return new HttpException(msg, exception)
                {
                    IsTimedOut = true
                };
            }

            return exception;
        }

        private void EnsureSuccessStatusCode(HttpWebResponse response, HttpRequestOptions options)
        {
            var statusCode = response.StatusCode;
            var isSuccessful = statusCode >= HttpStatusCode.OK && statusCode <= (HttpStatusCode)299;

            if (!isSuccessful)
            {
                if (options.LogErrorResponseBody)
                {
                    try
                    {
                        using (var stream = response.GetResponseStream())
                        {
                            if (stream != null)
                            {
                                using (var reader = new StreamReader(stream))
                                {
                                    var msg = reader.ReadToEnd();

                                    _logger.Error(msg);
                                }
                            }
                        }
                    }
                    catch
                    {

                    }
                }
                throw new HttpException(response.StatusDescription)
                {
                    StatusCode = response.StatusCode
                };
            }
        }

        /// <summary>
        /// Posts the specified URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="postData">The post data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        public Task<Stream> Post(string url, Dictionary<string, string> postData, CancellationToken cancellationToken)
        {
            return Post(url, postData, null, cancellationToken);
        }

        private Task<WebResponse> GetResponseAsync(WebRequest request, TimeSpan timeout)
        {
            var taskCompletion = new TaskCompletionSource<WebResponse>();

            Task<WebResponse> asyncTask = Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);

            ThreadPool.RegisterWaitForSingleObject((asyncTask as IAsyncResult).AsyncWaitHandle, TimeoutCallback, request, timeout, true);
            asyncTask.ContinueWith(task =>
            {
                taskCompletion.TrySetResult(task.Result);

            }, TaskContinuationOptions.NotOnFaulted);

            // Handle errors
            asyncTask.ContinueWith(task =>
            {
                if (task.Exception != null)
                {
                    taskCompletion.TrySetException(task.Exception);
                }
                else
                {
                    taskCompletion.TrySetException(new List<Exception>());
                }

            }, TaskContinuationOptions.OnlyOnFaulted);

            return taskCompletion.Task;
        }

        private static void TimeoutCallback(object state, bool timedOut)
        {
            if (timedOut)
            {
                WebRequest request = (WebRequest)state;
                if (state != null)
                {
                    request.Abort();
                }
            }
        }
    }
}
