using System;
using System.Collections.Generic;
using MediaBrowser.Model.QuickConnect;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Controller.QuickConnect
{
    /// <summary>
    /// Quick connect standard interface.
    /// </summary>
    public interface IQuickConnect
    {
        /// <summary>
        /// Gets or sets the length of user facing codes.
        /// </summary>
        public int CodeLength { get; set; }

        /// <summary>
        /// Gets or sets the string to prefix internal access tokens with.
        /// </summary>
        public string TokenNamePrefix { get; set; }

        /// <summary>
        /// Gets the current state of quick connect.
        /// </summary>
        public QuickConnectState State { get; }

        /// <summary>
        /// Gets or sets the time (in minutes) before a pending request will expire.
        /// </summary>
        public int RequestExpiry { get; set; }

        /// <summary>
        /// Assert that quick connect is currently active and throws an exception if it is not.
        /// </summary>
        void AssertActive();

        /// <summary>
        /// Temporarily activates quick connect for a short amount of time.
        /// </summary>
        /// <returns>A quick connect result object indicating success.</returns>
        QuickConnectResult Activate();

        /// <summary>
        /// Changes the status of quick connect.
        /// </summary>
        /// <param name="newState">New state to change to.</param>
        void SetEnabled(QuickConnectState newState);

        /// <summary>
        /// Initiates a new quick connect request.
        /// </summary>
        /// <param name="friendlyName">Friendly device name to display in the request UI.</param>
        /// <returns>A quick connect result with tokens to proceed or a descriptive error message otherwise.</returns>
        QuickConnectResult TryConnect(string friendlyName);

        /// <summary>
        /// Checks the status of an individual request.
        /// </summary>
        /// <param name="secret">Unique secret identifier of the request.</param>
        /// <returns>Quick connect result.</returns>
        QuickConnectResult CheckRequestStatus(string secret);

        /// <summary>
        /// Returns all current quick connect requests as DTOs. Does not include sensitive information.
        /// </summary>
        /// <returns>List of all quick connect results.</returns>
        List<QuickConnectResultDto> GetCurrentRequests();

        /// <summary>
        /// Returns all current quick connect requests (including sensitive information).
        /// </summary>
        /// <returns>List of all quick connect results.</returns>
        List<QuickConnectResult> GetCurrentRequestsInternal();

        /// <summary>
        /// Authorizes a quick connect request to connect as the calling user.
        /// </summary>
        /// <param name="request">HTTP request object.</param>
        /// <param name="lookup">Public request lookup value.</param>
        /// <returns>A boolean indicating if the authorization completed successfully.</returns>
        bool AuthorizeRequest(IRequest request, string lookup);

        /// <summary>
        /// Deletes all quick connect access tokens for the provided user.
        /// </summary>
        /// <param name="user">Guid of the user to delete tokens for.</param>
        /// <returns>A count of the deleted tokens.</returns>
        int DeleteAllDevices(Guid user);

        /// <summary>
        /// Generates a short code to display to the user to uniquely identify this request.
        /// </summary>
        /// <returns>A short, unique alphanumeric string.</returns>
        string GenerateCode();
    }
}
