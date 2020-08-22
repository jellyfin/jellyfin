#pragma warning disable CS1591

using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Security
{
    public interface IAuthenticationRepository
    {
        /// <summary>
        /// Creates the specified information.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns>Task.</returns>
        void Create(AuthenticationInfo info);

        /// <summary>
        /// Updates the specified information.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns>Task.</returns>
        void Update(AuthenticationInfo info);

        /// <summary>
        /// Gets the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>QueryResult{AuthenticationInfo}.</returns>
        QueryResult<AuthenticationInfo> Get(AuthenticationInfoQuery query);

        void Delete(AuthenticationInfo info);

        DeviceOptions GetDeviceOptions(string deviceId);

        void UpdateDeviceOptions(string deviceId, DeviceOptions options);
    }
}
