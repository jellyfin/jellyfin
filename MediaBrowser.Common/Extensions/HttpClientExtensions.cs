using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Json;

namespace MediaBrowser.Common.Extensions
{
    /// <summary>
    /// Http Client Extensions.
    /// </summary>
    public static class HttpClientExtensions
    {
        private static readonly JsonSerializerOptions _defaultJsonOptions = JsonDefaults.GetOptions();

        /// <summary>
        /// Read json from uri as T.
        /// </summary>
        /// <param name="client">The http client.</param>
        /// <param name="requestUri">The request uri.</param>
        /// <param name="jsonSerializerOptions">The json serializer options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <typeparam name="T">Type to deserialize json as.</typeparam>
        /// <returns>Deserialized json.</returns>
        public static async Task<T> ReadAsAsync<T>(
            this HttpClient client,
            string requestUri,
            JsonSerializerOptions jsonSerializerOptions = null,
            CancellationToken cancellationToken = default)
        {
            using var response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            return await response.ReadAsAsync<T>(jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Read json from http response message as T.
        /// </summary>
        /// <param name="responseMessage">The response message.</param>
        /// <param name="jsonSerializerOptions">The json serializer options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <typeparam name="T">Type to deserialize json as.</typeparam>
        /// <returns>Deserialized json.</returns>
        public static async Task<T> ReadAsAsync<T>(
            this HttpResponseMessage responseMessage,
            JsonSerializerOptions jsonSerializerOptions = null,
            CancellationToken cancellationToken = default)
        {
            responseMessage.EnsureSuccessStatusCode();
            await using var responseStream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<T>(responseStream, jsonSerializerOptions ?? _defaultJsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
