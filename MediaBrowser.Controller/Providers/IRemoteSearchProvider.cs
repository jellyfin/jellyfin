using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;

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
        Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken);
    }
}