using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.ApiInteraction
{
    /// <summary>
    /// Class AsyncHttpClient
    /// </summary>
    public class AsyncHttpClient : IAsyncHttpClient
    {
        /// <summary>
        /// Gets or sets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        private HttpClient HttpClient { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiClient" /> class.
        /// </summary>
        public AsyncHttpClient(HttpMessageHandler handler)
        {
            HttpClient = new HttpClient(handler);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiClient" /> class.
        /// </summary>
        public AsyncHttpClient()
        {
            HttpClient = new HttpClient();
        }

        /// <summary>
        /// Gets the stream async.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        /// <exception cref="MediaBrowser.Model.Net.HttpException"></exception>
        public async Task<Stream> GetAsync(string url, ILogger logger, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            logger.Debug("Sending Http Get to {0}", url);

            try
            {
                var msg = await HttpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

                EnsureSuccessStatusCode(msg);

                return await msg.Content.ReadAsStreamAsync().ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                logger.ErrorException("Error getting response from " + url, ex);

                throw new HttpException(ex.Message, ex);
            }
            catch (OperationCanceledException ex)
            {
                throw GetCancellationException(url, cancellationToken, ex, logger);
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error requesting {0}", ex, url);
                
                throw;
            }
        }

        /// <summary>
        /// Posts the async.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="postContent">Content of the post.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        /// <exception cref="MediaBrowser.Model.Net.HttpException"></exception>
        public async Task<Stream> PostAsync(string url, string contentType, string postContent, ILogger logger, CancellationToken cancellationToken)
        {
            logger.Debug("Sending Http Post to {0}", url);
            
            var content = new StringContent(postContent, Encoding.UTF8, contentType);

            try
            {
                var msg = await HttpClient.PostAsync(url, content).ConfigureAwait(false);

                EnsureSuccessStatusCode(msg);

                return await msg.Content.ReadAsStreamAsync().ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                logger.ErrorException("Error getting response from " + url, ex);

                throw new HttpException(ex.Message, ex);
            }
            catch (OperationCanceledException ex)
            {
                throw GetCancellationException(url, cancellationToken, ex, logger);
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error posting {0}", ex, url);

                throw;
            }
        }

        /// <summary>
        /// Deletes the async.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task DeleteAsync(string url, ILogger logger, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            logger.Debug("Sending Http Delete to {0}", url);

            try
            {
                using (var msg = await HttpClient.DeleteAsync(url, cancellationToken).ConfigureAwait(false))
                {
                    EnsureSuccessStatusCode(msg);
                }
            }
            catch (HttpRequestException ex)
            {
                logger.ErrorException("Error getting response from " + url, ex);

                throw new HttpException(ex.Message, ex);
            }
            catch (OperationCanceledException ex)
            {
                throw GetCancellationException(url, cancellationToken, ex, logger);
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error requesting {0}", ex, url);

                throw;
            }
        }

        /// <summary>
        /// Throws the cancellation exception.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>Exception.</returns>
        private Exception GetCancellationException(string url, CancellationToken cancellationToken, OperationCanceledException exception, ILogger logger)
        {
            // If the HttpClient's timeout is reached, it will cancel the Task internally
            if (!cancellationToken.IsCancellationRequested)
            {
                var msg = string.Format("Connection to {0} timed out", url);

                logger.Error(msg);

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
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                HttpClient.Dispose();
            }
        }

        /// <summary>
        /// Sets the authorization header that should be supplied on every request
        /// </summary>
        /// <param name="header">The header.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void SetAuthorizationHeader(string header)
        {
            if (string.IsNullOrEmpty(header))
            {
                HttpClient.DefaultRequestHeaders.Remove("Authorization");
            }
            else
            {
                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("MediaBrowser", header);
            }
        }
    }
}
