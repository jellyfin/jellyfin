using MediaBrowser.Model.Devices;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Model.ApiClient
{
    public interface IDevice
    {
        /// <summary>
        /// Occurs when [resume from sleep].
        /// </summary>
        event EventHandler<EventArgs> ResumeFromSleep;
        /// <summary>
        /// Gets the name of the device.
        /// </summary>
        /// <value>The name of the device.</value>
        string DeviceName { get; }
        /// <summary>
        /// Gets the device identifier.
        /// </summary>
        /// <value>The device identifier.</value>
        string DeviceId { get; }
        /// <summary>
        /// Gets the local images.
        /// </summary>
        /// <returns>IEnumerable&lt;LocalFileInfo&gt;.</returns>
        Task<IEnumerable<LocalFileInfo>> GetLocalPhotos();
        /// <summary>
        /// Gets the local videos.
        /// </summary>
        /// <returns>IEnumerable&lt;LocalFileInfo&gt;.</returns>
        Task<IEnumerable<LocalFileInfo>> GetLocalVideos();
        /// <summary>
        /// Uploads the file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="apiClient">The API client.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task UploadFile(LocalFileInfo file, IApiClient apiClient, CancellationToken cancellationToken = default(CancellationToken));
    }
}
