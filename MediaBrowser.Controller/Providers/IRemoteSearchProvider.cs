#pragma warning disable CS1591

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    public interface IRemoteSearchProvider : IMetadataProvider
    {
        /// <summary>
        /// Gets the image response.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{HttpResponseInfo}.</returns>
        Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken);
    }
}
