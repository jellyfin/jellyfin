using System.Net.Http.Headers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Cache;
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
        
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientManager" /> class.
        /// </summary>
        /// <param name="appPaths">The kernel.</param>
        /// <param name="logger">The logger.</param>
        public HttpClientManager(IApplicationPaths appPaths, ILogger logger)
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
            _appPaths = appPaths;
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
        /// <param name="host">The host.</param>
        /// <returns>HttpClient.</returns>
        /// <exception cref="System.ArgumentNullException">host</exception>
        private HttpClient GetHttpClient(string host)
        {
            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentNullException("host");
            }

            HttpClient client;
            if (!_httpClients.TryGetValue(host, out client))
            {
                var handler = new WebRequestHandler
                {
                    //AutomaticDecompression = DecompressionMethods.Deflate,
                    CachePolicy = new RequestCachePolicy(RequestCacheLevel.Revalidate)
                };

                client = new HttpClient(handler);
                client.DefaultRequestHeaders.Add("Accept", "application/json,image/*");
                client.Timeout = TimeSpan.FromSeconds(15);
                _httpClients.TryAdd(host, client);
            }

            return client;
        }

        /// <summary>
        /// Performs a GET request and returns the resulting stream
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="resourcePool">The resource pool.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        /// <exception cref="MediaBrowser.Model.Net.HttpException"></exception>
        public async Task<Stream> Get(string url, SemaphoreSlim resourcePool, CancellationToken cancellationToken)
        {
            ValidateParams(url, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (resourcePool != null)
            {
                await resourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            _logger.Info("HttpClientManager.Get url: {0}", url);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var msg = await GetHttpClient(GetHostFromUrl(url)).GetAsync(url, cancellationToken).ConfigureAwait(false);

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

                var msg = await GetHttpClient(GetHostFromUrl(url)).PostAsync(url, content, cancellationToken).ConfigureAwait(false);

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
        public Task<string> GetTempFile(HttpRequestOptions options)
        {
            var tempFile = Path.Combine(_appPaths.TempDirectory, Guid.NewGuid() + ".tmp");

            return GetTempFile(options, tempFile, 0);
        }

        /// <summary>
        /// Gets the temp file.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="tempFile">The temp file.</param>
        /// <param name="resumeCount">The resume count.</param>
        /// <returns>Task{System.String}.</returns>
        /// <exception cref="System.ArgumentNullException">progress</exception>
        /// <exception cref="HttpException"></exception>
        private async Task<string> GetTempFile(HttpRequestOptions options, string tempFile, int resumeCount)
        {
            ValidateParams(options.Url, options.CancellationToken);

            if (options.Progress == null)
            {
                throw new ArgumentNullException("progress");
            }

            options.CancellationToken.ThrowIfCancellationRequested();

            var message = new HttpRequestMessage(HttpMethod.Get, options.Url);

            if (!string.IsNullOrEmpty(options.UserAgent))
            {
                message.Headers.Add("User-Agent", options.UserAgent);
            }

            if (options.ResourcePool != null)
            {
                await options.ResourcePool.WaitAsync(options.CancellationToken).ConfigureAwait(false);
            }

            options.Progress.Report(0);
            
            _logger.Info("HttpClientManager.GetTempFile url: {0}, temp file: {1}", options.Url, tempFile);

            FileStream tempFileStream;

            if (resumeCount > 0 && File.Exists(tempFile))
            {
                tempFileStream = new FileStream(tempFile, FileMode.Open, FileAccess.Write, FileShare.Read,
                                                StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous);

                var startPosition = tempFileStream.Length;
                tempFileStream.Seek(startPosition, SeekOrigin.Current);

                message.Headers.Range = new RangeHeaderValue(startPosition, null);

                _logger.Info("Resuming from position {1} for {0}", options.Url, startPosition);
            }
            else
            {
                tempFileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.Read,
                                                StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous);
            }

            var serverSupportsRangeRequests = false;

            Exception downloadException = null;

            try
            {
                options.CancellationToken.ThrowIfCancellationRequested();

                using (var response = await GetHttpClient(GetHostFromUrl(options.Url)).SendAsync(message, HttpCompletionOption.ResponseHeadersRead, options.CancellationToken).ConfigureAwait(false))
                {
                    EnsureSuccessStatusCode(response);

                    options.CancellationToken.ThrowIfCancellationRequested();

                    var rangeValue = string.Join(" ", response.Headers.AcceptRanges.ToArray());
                    serverSupportsRangeRequests = rangeValue.IndexOf("bytes", StringComparison.OrdinalIgnoreCase) != -1 || rangeValue.IndexOf("*", StringComparison.OrdinalIgnoreCase) != -1;

                    if (!serverSupportsRangeRequests && resumeCount > 0)
                    {
                        _logger.Info("Server does not support range requests for {0}", options.Url);
                        tempFileStream.Position = 0;
                    }

                    IEnumerable<string> lengthValues;

                    if (!response.Headers.TryGetValues("content-length", out lengthValues) &&
                        !response.Content.Headers.TryGetValues("content-length", out lengthValues))
                    {
                        // We're not able to track progress
                        using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            await stream.CopyToAsync(tempFileStream, StreamDefaults.DefaultCopyToBufferSize, options.CancellationToken).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        var length = long.Parse(string.Join(string.Empty, lengthValues.ToArray()));

                        using (var stream = ProgressStream.CreateReadProgressStream(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), options.Progress.Report, length))
                        {
                            await stream.CopyToAsync(tempFileStream, StreamDefaults.DefaultCopyToBufferSize, options.CancellationToken).ConfigureAwait(false);
                        }
                    }

                    options.Progress.Report(100);

                    options.CancellationToken.ThrowIfCancellationRequested();
                }
            }
            catch (Exception ex)
            {
                downloadException = ex;
            }
            finally
            {
                tempFileStream.Dispose();

                if (options.ResourcePool != null)
                {
                    options.ResourcePool.Release();
                }
            }

            if (downloadException != null)
            {
                await HandleTempFileException(downloadException, options, tempFile, serverSupportsRangeRequests, resumeCount).ConfigureAwait(false);
            }

            return tempFile;
        }

        /// <summary>
        /// Handles the temp file exception.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <param name="options">The options.</param>
        /// <param name="tempFile">The temp file.</param>
        /// <param name="serverSupportsRangeRequests">if set to <c>true</c> [server supports range requests].</param>
        /// <param name="resumeCount">The resume count.</param>
        /// <returns>Task.</returns>
        /// <exception cref="HttpException"></exception>
        private Task HandleTempFileException(Exception ex, HttpRequestOptions options, string tempFile, bool serverSupportsRangeRequests, int resumeCount)
        {
            var operationCanceledException = ex as OperationCanceledException;

            if (operationCanceledException != null)
            {
                // Cleanup
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }

                throw GetCancellationException(options.Url, options.CancellationToken, operationCanceledException);
            }

            _logger.ErrorException("Error getting response from " + options.Url, ex);

            var httpRequestException = ex as HttpRequestException;

            // Cleanup
            if (File.Exists(tempFile))
            {
                // Try to resume
                if (httpRequestException != null && serverSupportsRangeRequests && resumeCount < options.MaxResumeCount && new FileInfo(tempFile).Length > 0)
                {
                    _logger.Info("Attempting to resume download from {0}", options.Url);

                    return GetTempFile(options, tempFile, resumeCount + 1);
                }

                File.Delete(tempFile);
            }

            if (httpRequestException != null)
            {
                throw new HttpException(ex.Message, ex);
            }

            throw ex;
        }
        
        /// <summary>
        /// Downloads the contents of a given url into a MemoryStream
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="resourcePool">The resource pool.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{MemoryStream}.</returns>
        /// <exception cref="MediaBrowser.Model.Net.HttpException"></exception>
        public async Task<MemoryStream> GetMemoryStream(string url, SemaphoreSlim resourcePool, CancellationToken cancellationToken)
        {
            ValidateParams(url, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            var message = new HttpRequestMessage(HttpMethod.Get, url);

            if (resourcePool != null)
            {
                await resourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            var ms = new MemoryStream();

            _logger.Info("HttpClientManager.GetMemoryStream url: {0}", url);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var response = await GetHttpClient(GetHostFromUrl(url)).SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    EnsureSuccessStatusCode(response);

                    cancellationToken.ThrowIfCancellationRequested();

                    using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        await stream.CopyToAsync(ms, StreamDefaults.DefaultCopyToBufferSize, cancellationToken).ConfigureAwait(false);
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }

                ms.Position = 0;

                return ms;
            }
            catch (OperationCanceledException ex)
            {
                ms.Dispose();

                throw GetCancellationException(url, cancellationToken, ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.ErrorException("Error getting response from " + url, ex);

                ms.Dispose();

                throw new HttpException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error getting response from " + url, ex);

                ms.Dispose();

                throw;
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
                    client.Dispose();
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

        /// <summary>
        /// Gets the memory stream.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{MemoryStream}.</returns>
        public Task<MemoryStream> GetMemoryStream(string url, CancellationToken cancellationToken)
        {
            return GetMemoryStream(url, null, cancellationToken);
        }
    }
}
