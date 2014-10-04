using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Users;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Model.ApiClient
{
    public interface IConnectionManager
    {
        /// <summary>
        /// Occurs when [connected].
        /// </summary>
        event EventHandler<GenericEventArgs<ConnectionResult>> Connected;

        /// <summary>
        /// Gets the API client.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>MediaBrowser.Model.ApiClient.IApiClient.</returns>
        IApiClient GetApiClient(BaseItemDto item);

        /// <summary>
        /// Connects the specified cancellation token.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;ConnectionResult&gt;.</returns>
        Task<ConnectionResult> Connect(CancellationToken cancellationToken);
        
        /// <summary>
        /// Connects the specified server.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;ConnectionResult&gt;.</returns>
        Task<ConnectionResult> Connect(ServerInfo server, CancellationToken cancellationToken);

        /// <summary>
        /// Connects the specified server.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;ConnectionResult&gt;.</returns>
        Task<ConnectionResult> Connect(string address, CancellationToken cancellationToken);

        /// <summary>
        /// Logouts this instance.
        /// </summary>
        /// <returns>Task&lt;ConnectionResult&gt;.</returns>
        Task<ConnectionResult> Logout();

        /// <summary>
        /// Authenticates with a specific server
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="username">The username.</param>
        /// <param name="hash">The hash.</param>
        /// <param name="rememberLogin">if set to <c>true</c> [remember login].</param>
        /// <returns>Task.</returns>
        Task<AuthenticationResult> Authenticate(ServerInfo server, string username, byte[] hash, bool rememberLogin);

        /// <summary>
        /// Authenticates with a specific server
        /// </summary>
        /// <param name="apiClient">The API client.</param>
        /// <param name="username">The username.</param>
        /// <param name="hash">The hash.</param>
        /// <param name="rememberLogin">if set to <c>true</c> [remember login].</param>
        /// <returns>Task.</returns>
        Task<AuthenticationResult> Authenticate(IApiClient apiClient, string username, byte[] hash, bool rememberLogin);
    }
}
