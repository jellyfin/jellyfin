#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities.Security;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.Session
{
    /// <summary>
    /// Interface ISessionManager.
    /// </summary>
    public interface ISessionManager
    {
        /// <summary>
        /// Occurs when [playback start].
        /// </summary>
        event EventHandler<PlaybackProgressEventArgs> PlaybackStart;

        /// <summary>
        /// Occurs when [playback progress].
        /// </summary>
        event EventHandler<PlaybackProgressEventArgs> PlaybackProgress;

        /// <summary>
        /// Occurs when [playback stopped].
        /// </summary>
        event EventHandler<PlaybackStopEventArgs> PlaybackStopped;

        /// <summary>
        /// Occurs when [session started].
        /// </summary>
        event EventHandler<SessionEventArgs> SessionStarted;

        /// <summary>
        /// Occurs when [session ended].
        /// </summary>
        event EventHandler<SessionEventArgs> SessionEnded;

        event EventHandler<SessionEventArgs> SessionActivity;

        /// <summary>
        /// Occurs when [session controller connected].
        /// </summary>
        event EventHandler<SessionEventArgs> SessionControllerConnected;

        /// <summary>
        /// Occurs when [capabilities changed].
        /// </summary>
        event EventHandler<SessionEventArgs> CapabilitiesChanged;

        /// <summary>
        /// Gets the sessions.
        /// </summary>
        /// <value>The sessions.</value>
        IEnumerable<SessionInfo> Sessions { get; }

        /// <summary>
        /// Logs the user activity.
        /// </summary>
        /// <param name="appName">Type of the client.</param>
        /// <param name="appVersion">The app version.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <param name="user">The user.</param>
        /// <returns>A task containing the session information.</returns>
        Task<SessionInfo> LogSessionActivity(string appName, string appVersion, string deviceId, string deviceName, string remoteEndPoint, Jellyfin.Data.Entities.User user);

        /// <summary>
        /// Used to report that a session controller has connected.
        /// </summary>
        /// <param name="session">The session.</param>
        void OnSessionControllerConnected(SessionInfo session);

        void UpdateDeviceName(string sessionId, string reportedDeviceName);

        /// <summary>
        /// Used to report that playback has started for an item.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>Task.</returns>
        Task OnPlaybackStart(PlaybackStartInfo info);

        /// <summary>
        /// Used to report playback progress for an item.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">Throws if an argument is null.</exception>
        Task OnPlaybackProgress(PlaybackProgressInfo info);

        Task OnPlaybackProgress(PlaybackProgressInfo info, bool isAutomated);

        /// <summary>
        /// Used to report that playback has ended for an item.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">Throws if an argument is null.</exception>
        Task OnPlaybackStopped(PlaybackStopInfo info);

        /// <summary>
        /// Reports the session ended.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns>Task.</returns>
        ValueTask ReportSessionEnded(string sessionId);

        /// <summary>
        /// Sends the general command.
        /// </summary>
        /// <param name="controllingSessionId">The controlling session identifier.</param>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendGeneralCommand(string controllingSessionId, string sessionId, GeneralCommand command, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the message command.
        /// </summary>
        /// <param name="controllingSessionId">The controlling session identifier.</param>
        /// <param name="sessionId">The session id.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendMessageCommand(string controllingSessionId, string sessionId, MessageCommand command, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the play command.
        /// </summary>
        /// <param name="controllingSessionId">The controlling session identifier.</param>
        /// <param name="sessionId">The session id.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendPlayCommand(string controllingSessionId, string sessionId, PlayRequest command, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a SyncPlayCommand to a session.
        /// </summary>
        /// <param name="sessionId">The identifier of the session.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendSyncPlayCommand(string sessionId, SendCommand command, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a SyncPlayGroupUpdate to a session.
        /// </summary>
        /// <param name="sessionId">The identifier of the session.</param>
        /// <param name="command">The group update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <typeparam name="T">Type of group.</typeparam>
        /// <returns>Task.</returns>
        Task SendSyncPlayGroupUpdate<T>(string sessionId, GroupUpdate<T> command, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the browse command.
        /// </summary>
        /// <param name="controllingSessionId">The controlling session identifier.</param>
        /// <param name="sessionId">The session id.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendBrowseCommand(string controllingSessionId, string sessionId, BrowseRequest command, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the playstate command.
        /// </summary>
        /// <param name="controllingSessionId">The controlling session identifier.</param>
        /// <param name="sessionId">The session id.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendPlaystateCommand(string controllingSessionId, string sessionId, PlaystateRequest command, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the message to admin sessions.
        /// </summary>
        /// <typeparam name="T">Type of data.</typeparam>
        /// <param name="name">Message type name.</param>
        /// <param name="data">The data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendMessageToAdminSessions<T>(SessionMessageType name, T data, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the message to user sessions.
        /// </summary>
        /// <typeparam name="T">Type of data.</typeparam>
        /// <param name="userIds">Users to send messages to.</param>
        /// <param name="name">Message type name.</param>
        /// <param name="data">The data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendMessageToUserSessions<T>(List<Guid> userIds, SessionMessageType name, T data, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the message to user sessions.
        /// </summary>
        /// <typeparam name="T">Type of data.</typeparam>
        /// <param name="userIds">Users to send messages to.</param>
        /// <param name="name">Message type name.</param>
        /// <param name="dataFn">Data function.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendMessageToUserSessions<T>(List<Guid> userIds, SessionMessageType name, Func<T> dataFn, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the message to user device sessions.
        /// </summary>
        /// <typeparam name="T">Type of data.</typeparam>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="name">Message type name.</param>
        /// <param name="data">The data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendMessageToUserDeviceSessions<T>(string deviceId, SessionMessageType name, T data, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the restart required message.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendRestartRequiredNotification(CancellationToken cancellationToken);

        /// <summary>
        /// Adds the additional user.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="userId">The user identifier.</param>
        void AddAdditionalUser(string sessionId, Guid userId);

        /// <summary>
        /// Removes the additional user.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="userId">The user identifier.</param>
        void RemoveAdditionalUser(string sessionId, Guid userId);

        /// <summary>
        /// Reports the now viewing item.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="itemId">The item identifier.</param>
        void ReportNowViewingItem(string sessionId, string itemId);

        /// <summary>
        /// Authenticates the new session.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{SessionInfo}.</returns>
        Task<AuthenticationResult> AuthenticateNewSession(AuthenticationRequest request);

        Task<AuthenticationResult> AuthenticateDirect(AuthenticationRequest request);

        /// <summary>
        /// Reports the capabilities.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="capabilities">The capabilities.</param>
        void ReportCapabilities(string sessionId, ClientCapabilities capabilities);

        /// <summary>
        /// Reports the transcoding information.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="info">The information.</param>
        void ReportTranscodingInfo(string deviceId, TranscodingInfo info);

        /// <summary>
        /// Clears the transcoding information.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        void ClearTranscodingInfo(string deviceId);

        /// <summary>
        /// Gets the session.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="client">The client.</param>
        /// <param name="version">The version.</param>
        /// <returns>SessionInfo.</returns>
        SessionInfo GetSession(string deviceId, string client, string version);

        /// <summary>
        /// Gets all sessions available to a user.
        /// </summary>
        /// <param name="userId">The session identifier.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="activeWithinSeconds">Active within session limit.</param>
        /// <param name="controllableUserToCheck">Filter for sessions remote controllable for this user.</param>
        /// <param name="isApiKey">Is the request authenticated with API key.</param>
        /// <returns>IReadOnlyList{SessionInfoDto}.</returns>
        IReadOnlyList<SessionInfoDto> GetSessions(Guid userId, string deviceId, int? activeWithinSeconds, Guid? controllableUserToCheck, bool isApiKey);

        /// <summary>
        /// Gets the session by authentication token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="remoteEndpoint">The remote endpoint.</param>
        /// <returns>SessionInfo.</returns>
        Task<SessionInfo> GetSessionByAuthenticationToken(string token, string deviceId, string remoteEndpoint);

        /// <summary>
        /// Gets the session by authentication token.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="remoteEndpoint">The remote endpoint.</param>
        /// <param name="appVersion">The application version.</param>
        /// <returns>Task&lt;SessionInfo&gt;.</returns>
        Task<SessionInfo> GetSessionByAuthenticationToken(Device info, string deviceId, string remoteEndpoint, string appVersion);

        /// <summary>
        /// Logs out the specified access token.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <returns>A <see cref="Task"/> representing the log out process.</returns>
        Task Logout(string accessToken);

        Task Logout(Device device);

        /// <summary>
        /// Revokes the user tokens.
        /// </summary>
        /// <param name="userId">The user's id.</param>
        /// <param name="currentAccessToken">The current access token.</param>
        /// <returns>Task.</returns>
        Task RevokeUserTokens(Guid userId, string currentAccessToken);

        Task CloseIfNeededAsync(SessionInfo session);
    }
}
