using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The _app paths
        /// </summary>
        private readonly IApplicationPaths _appPaths;

        public delegate HttpMessageHandler GetHttpMessageHandler(bool enableHttpCompression);

        private readonly GetHttpMessageHandler _getHttpMessageHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientManager" /> class.
        /// </summary>
        /// <param name="appPaths">The kernel.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.ArgumentNullException">
        /// appPaths
        /// or
        /// logger
        /// </exception>
        public HttpClientManager(IApplicationPaths appPaths, ILogger logger, GetHttpMessageHandler getHttpMessageHandler)
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
            _getHttpMessageHandler = getHttpMessageHandler;
            _appPaths = appPaths;
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
                client = new HttpClientInfo
                {
                    HttpClient = new HttpClient(_getHttpMessageHandler(enableHttpCompression))
                    {
                        Timeout = TimeSpan.FromSeconds(20)
                    }
                };
                _httpClients.TryAdd(key, client);
            }

            return client;
        }

        public async Task<HttpResponseInfo> GetResponse(HttpRequestOptions options)
        {
            ValidateParams(options.Url, options.CancellationToken);

            options.CancellationToken.ThrowIfCancellationRequested();

            var client = GetHttpClient(GetHostFromUrl(options.Url), options.EnableHttpCompression);

            if ((DateTime.UtcNow - client.LastTimeout).TotalSeconds < 30)
            {
                throw new HttpException(string.Format("Cancelling connection to {0} due to a previous timeout.", options.Url)) { IsTimedOut = true };
            }

            using (var message = GetHttpRequestMessage(options))
            {
                if (options.ResourcePool != null)
                {
                    await options.ResourcePool.WaitAsync(options.CancellationToken).ConfigureAwait(false);
                }

                if ((DateTime.UtcNow - client.LastTimeout).TotalSeconds < 30)
                {
                    if (options.ResourcePool != null)
                    {
                        options.ResourcePool.Release();
                    }

                    throw new HttpException(string.Format("Connection to {0} timed out", options.Url)) { IsTimedOut = true };
                }

                _logger.Info("HttpClientManager.Get url: {0}", options.Url);

                try
                {
                    options.CancellationToken.ThrowIfCancellationRequested();

                    var response = await client.HttpClient.SendAsync(message, HttpCompletionOption.ResponseContentRead, options.CancellationToken).ConfigureAwait(false);

                    EnsureSuccessStatusCode(response);

                    options.CancellationToken.ThrowIfCancellationRequested();

                    return new HttpResponseInfo
                    {
                        Content = await response.Content.ReadAsStreamAsync().ConfigureAwait(false),

                        StatusCode = response.StatusCode,

                        ContentType = response.Content.Headers.ContentType.MediaType
                    };
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
                catch (HttpRequestException ex)
                {
                    _logger.ErrorException("Error getting response from " + options.Url, ex);

                    throw new HttpException(ex.Message, ex);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error getting response from " + options.Url, ex);

                    throw;
                }
                finally
                {
                    if (options.ResourcePool != null)
                    {
                        options.ResourcePool.Release();
                    }
                }
            }
        }

        /// <summary>
        /// Performs a GET request and returns the resulting stream
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>Task{Stream}.</returns>
        /// <exception cref="HttpException"></exception>
        /// <exception cref="MediaBrowser.Model.Net.HttpException"></exception>
        public async Task<Stream> Get(HttpRequestOptions options)
        {
            ValidateParams(options.Url, options.CancellationToken);

            options.CancellationToken.ThrowIfCancellationRequested();

            var client = GetHttpClient(GetHostFromUrl(options.Url), options.EnableHttpCompression);

            if ((DateTime.UtcNow - client.LastTimeout).TotalSeconds < 30)
            {
                throw new HttpException(string.Format("Cancelling connection to {0} due to a previous timeout.", options.Url)) { IsTimedOut = true };
            }

            using (var message = GetHttpRequestMessage(options))
            {
                if (options.ResourcePool != null)
                {
                    await options.ResourcePool.WaitAsync(options.CancellationToken).ConfigureAwait(false);
                }

                if ((DateTime.UtcNow - client.LastTimeout).TotalSeconds < 30)
                {
                    if (options.ResourcePool != null)
                    {
                        options.ResourcePool.Release();
                    }
                    
                    throw new HttpException(string.Format("Connection to {0} timed out", options.Url)) { IsTimedOut = true };
                }
                
                _logger.Info("HttpClientManager.Get url: {0}", options.Url);

                try
                {
                    options.CancellationToken.ThrowIfCancellationRequested();

                    var response = await client.HttpClient.SendAsync(message, HttpCompletionOption.ResponseContentRead, options.CancellationToken).ConfigureAwait(false);

                    EnsureSuccessStatusCode(response);

                    options.CancellationToken.ThrowIfCancellationRequested();

                    return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
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
                catch (HttpRequestException ex)
                {
                    _logger.ErrorException("Error getting response from " + options.Url, ex);

                    throw new HttpException(ex.Message, ex);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error getting response from " + options.Url, ex);

                    throw;
                }
                finally
                {
                    if (options.ResourcePool != null)
                    {
                        options.ResourcePool.Release();
                    }
                }
            }
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
        /// Performs a POST request
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="postData">Params to add to the POST data.</param>
        /// <param name="resourcePool">The resource pool.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>stream on success, null on failure</returns>
        /// <exception cref="System.ArgumentNullException">postData</exception>
        /// <exception cref="MediaBrowser.Model.Net.HttpException"></exception>
        public async Task<Stream> Post(string url, Dictionary<string, string> postData, SemaphoreSlim resourcePool, CancellationToken cancellationToken)
        {
            ValidateParams(url, cancellationToken);

            if (postData == null)
            {
                throw new ArgumentNullException("postData");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var strings = postData.Keys.Select(key => string.Format("{0}={1}", key, postData[key]));
            var postContent = string.Join("&", strings.ToArray());
            var content = new StringContent(postContent, Encoding.UTF8, "application/x-www-form-urlencoded");

            if (resourcePool != null)
            {
                await resourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            _logger.Info("HttpClientManager.Post url: {0}", url);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var msg = await GetHttpClient(GetHostFromUrl(url), true).HttpClient.PostAsync(url, content, cancellationToken).ConfigureAwait(false);

                EnsureSuccessStatusCode(msg);

                return await msg.Content.ReadAsStreamAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                throw GetCancellationException(url, cancellationToken, ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.ErrorException("Error getting response from " + url, ex);

                throw new HttpException(ex.Message, ex);
            }
            finally
            {
                if (resourcePool != null)
                {
                    resourcePool.Release();
                }
            }
        }

        /// <summary>
        /// Downloads the contents of a given url into a temporary location
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>Task{System.String}.</returns>
        /// <exception cref="System.ArgumentNullException">progress</exception>
        /// <exception cref="HttpException"></exception>
        /// <exception cref="MediaBrowser.Model.Net.HttpException"></exception>
        public async Task<string> GetTempFile(HttpRequestOptions options)
        {
            var response = await GetTempFileResponse(options).ConfigureAwait(false);

            return response.TempFilePath;
        }

        public async Task<HttpResponseInfo> GetTempFileResponse(HttpRequestOptions options)
        {
            ValidateParams(options.Url, options.CancellationToken);

            var tempFile = Path.Combine(_appPaths.TempDirectory, Guid.NewGuid() + ".tmp");

            if (options.Progress == null)
            {
                throw new ArgumentNullException("progress");
            }

            options.CancellationToken.ThrowIfCancellationRequested();

            if (options.ResourcePool != null)
            {
                await options.ResourcePool.WaitAsync(options.CancellationToken).ConfigureAwait(false);
            }

            options.Progress.Report(0);

            _logger.Info("HttpClientManager.GetTempFile url: {0}, temp file: {1}", options.Url, tempFile);

            try
            {
                options.CancellationToken.ThrowIfCancellationRequested();

                using (var message = GetHttpRequestMessage(options))
                {
                    using (var response = await GetHttpClient(GetHostFromUrl(options.Url), options.EnableHttpCompression).HttpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, options.CancellationToken).ConfigureAwait(false))
                    {
                        EnsureSuccessStatusCode(response);

                        options.CancellationToken.ThrowIfCancellationRequested();

                        var contentLength = GetContentLength(response);

                        if (!contentLength.HasValue)
                        {
                            // We're not able to track progress
                            using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                            {
                                using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous))
                                {
                                    await stream.CopyToAsync(fs, StreamDefaults.DefaultCopyToBufferSize, options.CancellationToken).ConfigureAwait(false);
                                }
                            }
                        }
                        else
                        {
                            using (var stream = ProgressStream.CreateReadProgressStream(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), options.Progress.Report, contentLength.Value))
                            {
                                using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous))
                                {
                                    await stream.CopyToAsync(fs, StreamDefaults.DefaultCopyToBufferSize, options.CancellationToken).ConfigureAwait(false);
                                }
                            }
                        }

                        options.Progress.Report(100);

                        return new HttpResponseInfo
                        {
                            TempFilePath = tempFile,

                            StatusCode = response.StatusCode,

                            ContentType = response.Content.Headers.ContentType.MediaType
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                throw GetTempFileException(ex, options, tempFile);
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
        /// Gets the message.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>HttpResponseMessage.</returns>
        private HttpRequestMessage GetHttpRequestMessage(HttpRequestOptions options)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, options.Url);

            foreach (var pair in options.RequestHeaders.ToList())
            {
                message.Headers.Add(pair.Key, pair.Value);
            }

            return message;
        }

        /// <summary>
        /// Gets the length of the content.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns>System.Nullable{System.Int64}.</returns>
        private long? GetContentLength(HttpResponseMessage response)
        {
            IEnumerable<string> lengthValues;

            if (!response.Headers.TryGetValues("content-length", out lengthValues) && !response.Content.Headers.TryGetValues("content-length", out lengthValues))
            {
                return null;
            }

            return long.Parse(string.Join(string.Empty, lengthValues.ToArray()), UsCulture);
        }

        protected static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Handles the temp file exception.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <param name="options">The options.</param>
        /// <param name="tempFile">The temp file.</param>
        /// <returns>Task.</returns>
        /// <exception cref="HttpException"></exception>
        private Exception GetTempFileException(Exception ex, HttpRequestOptions options, string tempFile)
        {
            var operationCanceledException = ex as OperationCanceledException;

            if (operationCanceledException != null)
            {
                // Cleanup
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }

                return GetCancellationException(options.Url, options.CancellationToken, operationCanceledException);
            }

            _logger.ErrorException("Error getting response from " + options.Url, ex);

            var httpRequestException = ex as HttpRequestException;

            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }

            if (httpRequestException != null)
            {
                return new HttpException(ex.Message, ex);
            }

            return ex;
        }

        /// <summary>
        /// Validates the params.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="System.ArgumentNullException">url</exception>
        private void ValidateParams(string url, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("url");
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
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
                foreach (var client in _httpClients.Values.ToList())
                {
                    client.HttpClient.Dispose();
                }

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
                return new HttpException(msg, exception) { IsTimedOut = true };
            }

            return exception;
        }

        /// <summary>
        /// Ensures the success status code.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <exception cref="MediaBrowser.Model.Net.HttpException"></exception>
        private void EnsureSuccessStatusCode(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpException(response.ReasonPhrase) { StatusCode = response.StatusCode };
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
    }
}
