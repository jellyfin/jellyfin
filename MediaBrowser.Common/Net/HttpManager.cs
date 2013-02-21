using MediaBrowser.Common.IO;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Model.Net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Class HttpManager
    /// </summary>
    public class HttpManager : BaseManager<IKernel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpManager" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        public HttpManager(IKernel kernel)
            : base(kernel)
        {
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
                    AutomaticDecompression = DecompressionMethods.Deflate,
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
            ValidateParams(url, resourcePool, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            await resourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            Logger.Info("HttpManager.Get url: {0}", url);

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
                Logger.ErrorException("Error getting response from " + url, ex);

                throw new HttpException(ex.Message, ex);
            }
            finally
            {
                resourcePool.Release();
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
            ValidateParams(url, resourcePool, cancellationToken);

            if (postData == null)
            {
                throw new ArgumentNullException("postData");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var strings = postData.Keys.Select(key => string.Format("{0}={1}", key, postData[key]));
            var postContent = string.Join("&", strings.ToArray());
            var content = new StringContent(postContent, Encoding.UTF8, "application/x-www-form-urlencoded");

            await resourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            Logger.Info("HttpManager.Post url: {0}", url);

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
                Logger.ErrorException("Error getting response from " + url, ex);

                throw new HttpException(ex.Message, ex);
            }
            finally
            {
                resourcePool.Release();
            }
        }

        /// <summary>
        /// Downloads the contents of a given url into a temporary location
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="resourcePool">The resource pool.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="userAgent">The user agent.</param>
        /// <returns>Task{System.String}.</returns>
        /// <exception cref="System.ArgumentNullException">progress</exception>
        /// <exception cref="MediaBrowser.Model.Net.HttpException"></exception>
        public async Task<string> FetchToTempFile(string url, SemaphoreSlim resourcePool, CancellationToken cancellationToken, IProgress<double> progress, string userAgent = null)
        {
            ValidateParams(url, resourcePool, cancellationToken);

            if (progress == null)
            {
                throw new ArgumentNullException("progress");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var tempFile = Path.Combine(Kernel.ApplicationPaths.TempDirectory, Guid.NewGuid() + ".tmp");

            var message = new HttpRequestMessage(HttpMethod.Get, url);

            if (!string.IsNullOrEmpty(userAgent))
            {
                message.Headers.Add("User-Agent", userAgent);
            }

            await resourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            Logger.Info("HttpManager.FetchToTempFile url: {0}, temp file: {1}", url, tempFile);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var response = await GetHttpClient(GetHostFromUrl(url)).SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    EnsureSuccessStatusCode(response);

                    cancellationToken.ThrowIfCancellationRequested();

                    IEnumerable<string> lengthValues;

                    if (!response.Headers.TryGetValues("content-length", out lengthValues) &&
                        !response.Content.Headers.TryGetValues("content-length", out lengthValues))
                    {
                        // We're not able to track progress
                        using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous))
                            {
                                await stream.CopyToAsync(fs, StreamDefaults.DefaultCopyToBufferSize, cancellationToken).ConfigureAwait(false);
                            }
                        }
                    }
                    else
                    {
                        var length = long.Parse(string.Join(string.Empty, lengthValues.ToArray()));

                        using (var stream = ProgressStream.CreateReadProgressStream(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), progress.Report, length))
                        {
                            using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous))
                            {
                                await stream.CopyToAsync(fs, StreamDefaults.DefaultCopyToBufferSize, cancellationToken).ConfigureAwait(false);
                            }
                        }
                    }

                    progress.Report(100);

                    cancellationToken.ThrowIfCancellationRequested();
                }

                return tempFile;
            }
            catch (OperationCanceledException ex)
            {
                // Cleanup
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }

                throw GetCancellationException(url, cancellationToken, ex);
            }
            catch (HttpRequestException ex)
            {
                Logger.ErrorException("Error getting response from " + url, ex);

                // Cleanup
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }

                throw new HttpException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error getting response from " + url, ex);

                // Cleanup
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }

                throw;
            }
            finally
            {
                resourcePool.Release();
            }
        }

        /// <summary>
        /// Downloads the contents of a given url into a MemoryStream
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="resourcePool">The resource pool.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{MemoryStream}.</returns>
        /// <exception cref="MediaBrowser.Model.Net.HttpException"></exception>
        public async Task<MemoryStream> FetchToMemoryStream(string url, SemaphoreSlim resourcePool, CancellationToken cancellationToken)
        {
            ValidateParams(url, resourcePool, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            var message = new HttpRequestMessage(HttpMethod.Get, url);

            await resourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            var ms = new MemoryStream();

            Logger.Info("HttpManager.FetchToMemoryStream url: {0}", url);

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
                Logger.ErrorException("Error getting response from " + url, ex);

                ms.Dispose();

                throw new HttpException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error getting response from " + url, ex);

                ms.Dispose();

                throw;
            }
            finally
            {
                resourcePool.Release();
            }
        }

        /// <summary>
        /// Validates the params.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="resourcePool">The resource pool.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="System.ArgumentNullException">url</exception>
        private void ValidateParams(string url, SemaphoreSlim resourcePool, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("url");
            }

            if (resourcePool == null)
            {
                throw new ArgumentNullException("resourcePool");
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
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool dispose)
        {
            if (dispose)
            {
                foreach (var client in _httpClients.Values.ToList())
                {
                    client.Dispose();
                }

                _httpClients.Clear();
            }

            base.Dispose(dispose);
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

                Logger.Error(msg);

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
    }
}
