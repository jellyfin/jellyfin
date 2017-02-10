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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emby.Common.Implementations.HttpClientManager;
using MediaBrowser.Model.IO;
using MediaBrowser.Common;

namespace Emby.Common.Implementations.HttpClientManager
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
        private readonly IMemoryStreamFactory _memoryStreamProvider;
        private readonly Func<string> _defaultUserAgentFn;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientManager" /> class.
        /// </summary>
        public HttpClientManager(IApplicationPaths appPaths, ILogger logger, IFileSystem fileSystem, IMemoryStreamFactory memoryStreamProvider, Func<string> defaultUserAgentFn)
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
            _memoryStreamProvider = memoryStreamProvider;
            _appPaths = appPaths;
            _defaultUserAgentFn = defaultUserAgentFn;

#if NET46
            // http://stackoverflow.com/questions/566437/http-post-returns-the-error-417-expectation-failed-c
            ServicePointManager.Expect100Continue = false;

            // Trakt requests sometimes fail without this
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls;
#endif    
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

        private WebRequest CreateWebRequest(string url)
        {
            try
            {
                return WebRequest.Create(url);
            }
            catch (NotSupportedException)
            {
                //Webrequest creation does fail on MONO randomly when using WebRequest.Create
                //the issue occurs in the GetCreator method here: http://www.oschina.net/code/explore/mono-2.8.1/mcs/class/System/System.Net/WebRequest.cs

                var type = Type.GetType("System.Net.HttpRequestCreator, System, Version=4.0.0.0,Culture=neutral, PublicKeyToken=b77a5c561934e089");
                var creator = Activator.CreateInstance(type, nonPublic: true) as IWebRequestCreate;
                return creator.Create(new Uri(url)) as HttpWebRequest;
            }
        }

        private void AddIpv4Option(HttpWebRequest request, HttpRequestOptions options)
        {
#if NET46
            request.ServicePoint.BindIPEndPointDelegate = (servicePount, remoteEndPoint, retryCount) =>
            {
                if (remoteEndPoint.AddressFamily == AddressFamily.InterNetwork)
                {
                    return new IPEndPoint(IPAddress.Any, 0);
                }
                throw new InvalidOperationException("no IPv4 address");
            };
#endif    
        }

        private WebRequest GetRequest(HttpRequestOptions options, string method)
        {
            var url = options.Url;

            var uriAddress = new Uri(url);
            var userInfo = uriAddress.UserInfo;
            if (!string.IsNullOrWhiteSpace(userInfo))
            {
                _logger.Info("Found userInfo in url: {0} ... url: {1}", userInfo, url);
                url = url.Replace(userInfo + "@", string.Empty);
            }

            var request = CreateWebRequest(url);
            var httpWebRequest = request as HttpWebRequest;

            if (httpWebRequest != null)
            {
                if (options.PreferIpv4)
                {
                    AddIpv4Option(httpWebRequest, options);
                }

                AddRequestHeaders(httpWebRequest, options);

#if NET46
                if (options.EnableHttpCompression)
                {
                    if (options.DecompressionMethod.HasValue)
                    {
                        httpWebRequest.AutomaticDecompression = options.DecompressionMethod.Value == CompressionMethod.Gzip
                            ? DecompressionMethods.GZip
                            : DecompressionMethods.Deflate;
                    }
                    else
                    {
                        httpWebRequest.AutomaticDecompression = DecompressionMethods.Deflate;
                    }
                }
                else
                {
                    httpWebRequest.AutomaticDecompression = DecompressionMethods.None;
                }
#endif    
            }



#if NET46
            request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.BypassCache);
#endif    

            if (httpWebRequest != null)
            {
                if (options.EnableKeepAlive)
                {
#if NET46
                    httpWebRequest.KeepAlive = true;
#endif    
                }
            }

            request.Method = method;
#if NET46
            request.Timeout = options.TimeoutMs;
#endif

            if (httpWebRequest != null)
            {
                if (!string.IsNullOrEmpty(options.Host))
                {
#if NET46
                    httpWebRequest.Host = options.Host;
#elif NETSTANDARD1_6
                    httpWebRequest.Headers["Host"] = options.Host;
#endif
                }

                if (!string.IsNullOrEmpty(options.Referer))
                {
#if NET46
                    httpWebRequest.Referer = options.Referer;
#elif NETSTANDARD1_6
                    httpWebRequest.Headers["Referer"] = options.Referer;
#endif
                }
            }

            if (!string.IsNullOrWhiteSpace(userInfo))
            {
                var parts = userInfo.Split(':');
                if (parts.Length == 2)
                {
                    request.Credentials = GetCredential(url, parts[0], parts[1]);
                    // TODO: .net core ??
#if NET46
                    request.PreAuthenticate = true;
#endif
                }
            }

            return request;
        }

        private CredentialCache GetCredential(string url, string username, string password)
        {
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
            CredentialCache credentialCache = new CredentialCache();
            credentialCache.Add(new Uri(url), "Basic", new NetworkCredential(username, password));
            return credentialCache;
        }

        private void AddRequestHeaders(HttpWebRequest request, HttpRequestOptions options)
        {
            var hasUserAgent = false;

            foreach (var header in options.RequestHeaders.ToList())
            {
                if (string.Equals(header.Key, "Accept", StringComparison.OrdinalIgnoreCase))
                {
                    request.Accept = header.Value;
                }
                else if (string.Equals(header.Key, "User-Agent", StringComparison.OrdinalIgnoreCase))
                {
                    SetUserAgent(request, header.Value);
                    hasUserAgent = true;
                }
                else
                {
#if NET46
                    request.Headers.Set(header.Key, header.Value);
#elif NETSTANDARD1_6
                    request.Headers[header.Key] = header.Value;
#endif
                }
            }

            if (!hasUserAgent && options.EnableDefaultUserAgent)
            {
                SetUserAgent(request, _defaultUserAgentFn());
            }
        }

        private void SetUserAgent(HttpWebRequest request, string userAgent)
        {
#if NET46
            request.UserAgent = userAgent;
#elif NETSTANDARD1_6
                    request.Headers["User-Agent"] = userAgent;
#endif
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
                BufferContent = resourcePool != null
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
            if (options.CacheMode == CacheMode.None)
            {
                return await SendAsyncInternal(options, httpMethod).ConfigureAwait(false);
            }

            var url = options.Url;
            var urlHash = url.ToLower().GetMD5().ToString("N");

            var responseCachePath = Path.Combine(_appPaths.CachePath, "httpclient", urlHash);

            var response = await GetCachedResponse(responseCachePath, options.CacheLength, url).ConfigureAwait(false);
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

        private async Task<HttpResponseInfo> GetCachedResponse(string responseCachePath, TimeSpan cacheLength, string url)
        {
            _logger.Info("Checking for cache file {0}", responseCachePath);

            try
            {
                if (_fileSystem.GetLastWriteTimeUtc(responseCachePath).Add(cacheLength) > DateTime.UtcNow)
                {
                    using (var stream = _fileSystem.GetFileStream(responseCachePath, FileOpenMode.Open, FileAccessMode.Read, FileShareMode.Read, true))
                    {
                        var memoryStream = _memoryStreamProvider.CreateNew();

                        await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
                        memoryStream.Position = 0;

                        return new HttpResponseInfo
                        {
                            ResponseUrl = url,
                            Content = memoryStream,
                            StatusCode = HttpStatusCode.OK,
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
            _fileSystem.CreateDirectory(Path.GetDirectoryName(responseCachePath));

            using (var responseStream = response.Content)
            {
                var memoryStream = _memoryStreamProvider.CreateNew();
                await responseStream.CopyToAsync(memoryStream).ConfigureAwait(false);
                memoryStream.Position = 0;

                using (var fileStream = _fileSystem.GetFileStream(responseCachePath, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.None, true))
                {
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

            var httpWebRequest = GetRequest(options, httpMethod);

            if (options.RequestContentBytes != null ||
                !string.IsNullOrEmpty(options.RequestContent) ||
                string.Equals(httpMethod, "post", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var bytes = options.RequestContentBytes ??
                        Encoding.UTF8.GetBytes(options.RequestContent ?? string.Empty);

                    httpWebRequest.ContentType = options.RequestContentType ?? "application/x-www-form-urlencoded";

#if NET46
                    httpWebRequest.ContentLength = bytes.Length;
#endif
                    (await httpWebRequest.GetRequestStreamAsync().ConfigureAwait(false)).Write(bytes, 0, bytes.Length);
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

                    EnsureSuccessStatusCode(client, httpResponse, options);

                    options.CancellationToken.ThrowIfCancellationRequested();

                    return GetResponseInfo(httpResponse, httpResponse.GetResponseStream(), GetContentLength(httpResponse), httpResponse);
                }

                using (var response = await GetResponseAsync(httpWebRequest, TimeSpan.FromMilliseconds(options.TimeoutMs)).ConfigureAwait(false))
                {
                    var httpResponse = (HttpWebResponse)response;

                    EnsureSuccessStatusCode(client, httpResponse, options);

                    options.CancellationToken.ThrowIfCancellationRequested();

                    using (var stream = httpResponse.GetResponseStream())
                    {
                        var memoryStream = _memoryStreamProvider.CreateNew();

                        await stream.CopyToAsync(memoryStream).ConfigureAwait(false);

                        memoryStream.Position = 0;

                        return GetResponseInfo(httpResponse, memoryStream, memoryStream.Length, null);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                throw GetCancellationException(options, client, options.CancellationToken, ex);
            }
            catch (Exception ex)
            {
                throw GetException(ex, options, client);
            }
            finally
            {
                if (options.ResourcePool != null)
                {
                    options.ResourcePool.Release();
                }
            }
        }

        private HttpResponseInfo GetResponseInfo(HttpWebResponse httpResponse, Stream content, long? contentLength, IDisposable disposable)
        {
            var responseInfo = new HttpResponseInfo(disposable)
            {
                Content = content,

                StatusCode = httpResponse.StatusCode,

                ContentType = httpResponse.ContentType,

                ContentLength = contentLength,

                ResponseUrl = httpResponse.ResponseUri.ToString()
            };

            if (httpResponse.Headers != null)
            {
                SetHeaders(httpResponse.Headers, responseInfo);
            }

            return responseInfo;
        }

        private HttpResponseInfo GetResponseInfo(HttpWebResponse httpResponse, string tempFile, long? contentLength)
        {
            var responseInfo = new HttpResponseInfo
            {
                TempFilePath = tempFile,

                StatusCode = httpResponse.StatusCode,

                ContentType = httpResponse.ContentType,

                ContentLength = contentLength
            };

            if (httpResponse.Headers != null)
            {
                SetHeaders(httpResponse.Headers, responseInfo);
            }

            return responseInfo;
        }

        private void SetHeaders(WebHeaderCollection headers, HttpResponseInfo responseInfo)
        {
            foreach (var key in headers.AllKeys)
            {
                responseInfo.Headers[key] = headers[key];
            }
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
                CancellationToken = cancellationToken,
                BufferContent = resourcePool != null

            }, postData);
        }

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

            _fileSystem.CreateDirectory(_appPaths.TempDirectory);

            var tempFile = Path.Combine(_appPaths.TempDirectory, Guid.NewGuid() + ".tmp");

            if (options.Progress == null)
            {
                throw new ArgumentNullException("progress");
            }

            options.CancellationToken.ThrowIfCancellationRequested();

            var httpWebRequest = GetRequest(options, "GET");

            if (options.ResourcePool != null)
            {
                await options.ResourcePool.WaitAsync(options.CancellationToken).ConfigureAwait(false);
            }

            options.Progress.Report(0);

            if (options.LogRequest)
            {
                _logger.Info("HttpClientManager.GetTempFileResponse url: {0}", options.Url);
            }

            var client = GetHttpClient(GetHostFromUrl(options.Url), options.EnableHttpCompression);

            try
            {
                options.CancellationToken.ThrowIfCancellationRequested();

                using (var response = await httpWebRequest.GetResponseAsync().ConfigureAwait(false))
                {
                    var httpResponse = (HttpWebResponse)response;

                    EnsureSuccessStatusCode(client, httpResponse, options);

                    options.CancellationToken.ThrowIfCancellationRequested();

                    var contentLength = GetContentLength(httpResponse);

                    if (!contentLength.HasValue)
                    {
                        // We're not able to track progress
                        using (var stream = httpResponse.GetResponseStream())
                        {
                            using (var fs = _fileSystem.GetFileStream(tempFile, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.Read, true))
                            {
                                await stream.CopyToAsync(fs, StreamDefaults.DefaultCopyToBufferSize, options.CancellationToken).ConfigureAwait(false);
                            }
                        }
                    }
                    else
                    {
                        using (var stream = ProgressStream.CreateReadProgressStream(httpResponse.GetResponseStream(), options.Progress.Report, contentLength.Value))
                        {
                            using (var fs = _fileSystem.GetFileStream(tempFile, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.Read, true))
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
                throw GetException(ex, options, client);
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

        private Exception GetException(Exception ex, HttpRequestOptions options, HttpClientInfo client)
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
                    _logger.ErrorException("Error getting response from " + options.Url, ex);
                }

                var exception = new HttpException(ex.Message, ex);

                var response = webException.Response as HttpWebResponse;
                if (response != null)
                {
                    exception.StatusCode = response.StatusCode;

                    if ((int)response.StatusCode == 429)
                    {
                        client.LastTimeout = DateTime.UtcNow;
                    }
                }

                return exception;
            }

            var operationCanceledException = ex as OperationCanceledException
                ?? ex.InnerException as OperationCanceledException;

            if (operationCanceledException != null)
            {
                return GetCancellationException(options, client, options.CancellationToken, operationCanceledException);
            }

            if (options.LogErrors)
            {
                _logger.ErrorException("Error getting response from " + options.Url, ex);
            }

            return ex;
        }

        private void DeleteTempFile(string file)
        {
            try
            {
                _fileSystem.DeleteFile(file);
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
        /// <param name="options">The options.</param>
        /// <param name="client">The client.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="exception">The exception.</param>
        /// <returns>Exception.</returns>
        private Exception GetCancellationException(HttpRequestOptions options, HttpClientInfo client, CancellationToken cancellationToken, OperationCanceledException exception)
        {
            // If the HttpClient's timeout is reached, it will cancel the Task internally
            if (!cancellationToken.IsCancellationRequested)
            {
                var msg = string.Format("Connection to {0} timed out", options.Url);

                if (options.LogErrors)
                {
                    _logger.Error(msg);
                }

                client.LastTimeout = DateTime.UtcNow;

                // Throw an HttpException so that the caller doesn't think it was cancelled by user code
                return new HttpException(msg, exception)
                {
                    IsTimedOut = true
                };
            }

            return exception;
        }

        private void EnsureSuccessStatusCode(HttpClientInfo client, HttpWebResponse response, HttpRequestOptions options)
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
#if NET46
            var taskCompletion = new TaskCompletionSource<WebResponse>();

            Task<WebResponse> asyncTask = Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);

            ThreadPool.RegisterWaitForSingleObject((asyncTask as IAsyncResult).AsyncWaitHandle, TimeoutCallback, request, timeout, true);
            var callback = new TaskCallback { taskCompletion = taskCompletion };
            asyncTask.ContinueWith(callback.OnSuccess, TaskContinuationOptions.NotOnFaulted);

            // Handle errors
            asyncTask.ContinueWith(callback.OnError, TaskContinuationOptions.OnlyOnFaulted);

            return taskCompletion.Task;
#endif

            return request.GetResponseAsync();
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

        private class TaskCallback
        {
            public TaskCompletionSource<WebResponse> taskCompletion;

            public void OnSuccess(Task<WebResponse> task)
            {
                taskCompletion.TrySetResult(task.Result);
            }

            public void OnError(Task<WebResponse> task)
            {
                if (task.Exception != null)
                {
                    taskCompletion.TrySetException(task.Exception);
                }
                else
                {
                    taskCompletion.TrySetException(new List<Exception>());
                }
            }
        }
    }
}
