using MediaBrowser.Model.Connect;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Events;
using System;
using System.Collections.Generic;
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
        /// Occurs when [local user sign in].
        /// </summary>
        event EventHandler<GenericEventArgs<UserDto>> LocalUserSignIn;
        /// <summary>
        /// Occurs when [connect user sign in].
        /// </summary>
        event EventHandler<GenericEventArgs<ConnectUser>> ConnectUserSignIn;
        /// <summary>
        /// Occurs when [local user sign out].
        /// </summary>
        event EventHandler<EventArgs> LocalUserSignOut;
        /// <summary>
        /// Occurs when [connect user sign out].
        /// </summary>
        event EventHandler<EventArgs> ConnectUserSignOut;

        /// <summary>
        /// Gets the connect user.
        /// </summary>
        /// <value>The connect user.</value>
        ConnectUser ConnectUser { get; }

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
        /// Logins to connect.
        /// </summary>
        /// <returns>Task.</returns>
        Task LoginToConnect(string username, string password);

        /// <summary>
        /// Gets the active api client instance
        /// </summary>
        [Obsolete]
        IApiClient CurrentApiClient { get; }

        /// <summary>
        /// Creates the pin.
        /// </summary>
        /// <returns>Task&lt;PinCreationResult&gt;.</returns>
        Task<PinCreationResult> CreatePin();

        /// <summary>
        /// Gets the pin status.
        /// </summary>
        /// <param name="pin">The pin.</param>
        /// <returns>Task&lt;PinStatusResult&gt;.</returns>
        Task<PinStatusResult> GetPinStatus(PinCreationResult pin);

        /// <summary>
        /// Exchanges the pin.
        /// </summary>
        /// <param name="pin">The pin.</param>
        /// <returns>Task.</returns>
        Task ExchangePin(PinCreationResult pin);

        /// <summary>
        /// Gets the available servers.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<ServerInfo>> GetAvailableServers(CancellationToken cancellationToken);
    }
}
