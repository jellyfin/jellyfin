using System;
using System.Threading.Tasks;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.QuickConnect;

namespace MediaBrowser.Controller.QuickConnect
{
    /// <summary>
    /// Quick connect standard interface.
    /// </summary>
    public interface IQuickConnect
    {
        /// <summary>
        /// Gets a value indicating whether quick connect is enabled or not.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Initiates a new quick connect request.
        /// </summary>
        /// <param name="authorizationInfo">The initiator authorization info.</param>
        /// <returns>A quick connect result with tokens to proceed or throws an exception if not active.</returns>
        QuickConnectResult TryConnect(AuthorizationInfo authorizationInfo);

        /// <summary>
        /// Checks the status of an individual request.
        /// </summary>
        /// <param name="secret">Unique secret identifier of the request.</param>
        /// <returns>Quick connect result.</returns>
        QuickConnectResult CheckRequestStatus(string secret);

        /// <summary>
        /// Authorizes a quick connect request to connect as the calling user.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <param name="code">Identifying code for the request.</param>
        /// <returns>A boolean indicating if the authorization completed successfully.</returns>
        Task<bool> AuthorizeRequest(Guid userId, string code);

        /// <summary>
        /// Gets the authorized request for the secret.
        /// </summary>
        /// <param name="secret">The secret.</param>
        /// <returns>The authentication result.</returns>
        AuthenticationResult GetAuthorizedRequest(string secret);
    }
}
