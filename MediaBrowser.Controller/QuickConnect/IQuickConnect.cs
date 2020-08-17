using System;
using MediaBrowser.Model.QuickConnect;

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
        int CodeLength { get; set; }

        /// <summary>
        /// Gets or sets the name of internal access tokens.
        /// </summary>
        string TokenName { get; set; }

        /// <summary>
        /// Gets the current state of quick connect.
        /// </summary>
        QuickConnectState State { get; }

        /// <summary>
        /// Gets or sets the time (in minutes) before quick connect will automatically deactivate.
        /// </summary>
        int Timeout { get; set; }

        /// <summary>
        /// Assert that quick connect is currently active and throws an exception if it is not.
        /// </summary>
        void AssertActive();

        /// <summary>
        /// Temporarily activates quick connect for a short amount of time.
        /// </summary>
        void Activate();

        /// <summary>
        /// Changes the state of quick connect.
        /// </summary>
        /// <param name="newState">New state to change to.</param>
        void SetState(QuickConnectState newState);

        /// <summary>
        /// Initiates a new quick connect request.
        /// </summary>
        /// <returns>A quick connect result with tokens to proceed or throws an exception if not active.</returns>
        QuickConnectResult TryConnect();

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
        bool AuthorizeRequest(Guid userId, string code);

        /// <summary>
        /// Expire quick connect requests that are over the time limit. If <paramref name="expireAll"/> is true, all requests are unconditionally expired.
        /// </summary>
        /// <param name="expireAll">If true, all requests will be expired.</param>
        void ExpireRequests(bool expireAll = false);

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
