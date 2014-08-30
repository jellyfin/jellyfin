using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Model.ApiClient
{
    /// <summary>
    /// Class ApiClientExtensions
    /// </summary>
    public static class ApiClientExtensions
    {
        /// <summary>
        /// Gets the image stream async.
        /// </summary>
        /// <param name="apiClient">The API client.</param>
        /// <param name="url">The URL.</param>
        /// <returns>Task{Stream}.</returns>
        public static Task<Stream> GetImageStreamAsync(this IApiClient apiClient, string url)
        {
            return apiClient.GetImageStreamAsync(url, CancellationToken.None);
        }

        public static Task<UserDto[]> GetPublicUsersAsync(this IApiClient apiClient)
        {
            return apiClient.GetPublicUsersAsync(CancellationToken.None);
        }

        public static Task<ItemsResult> GetItemsAsync(this IApiClient apiClient, ItemQuery query)
        {
            return apiClient.GetItemsAsync(query, CancellationToken.None);
        }
    }
}
