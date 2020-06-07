using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.Session
{
    /// <summary>
    /// Interface ISessionManager
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
        /// Occurs when [capabilities changed].
        /// </summary>
        event EventHandler<SessionEventArgs> CapabilitiesChanged;

        /// <summary>
        /// Occurs when [authentication failed].
        /// </summary>
        event EventHandler<GenericEventArgs<AuthenticationRequest>> AuthenticationFailed;

        /// <summary>
        /// Occurs when [authentication succeeded].
        /// </summary>
        event EventHandler<GenericEventArgs<AuthenticationResult>> AuthenticationSucceeded;

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
        SessionInfo LogSessionActivity(string appName, string appVersion, string deviceId, string deviceName, string remoteEndPoint, User user);

        void UpdateDeviceName(string sessionId, string reportedDeviceName);

        /// <summary>
        /// Used to report that playback has started for an item
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>Task.</returns>
        Task OnPlaybackStart(PlaybackStartInfo info);

        /// <summary>
        /// Used to report playback progress for an item
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        Task OnPlaybackProgress(PlaybackProgressInfo info);

        Task OnPlaybackProgress(PlaybackProgressInfo info, bool isAutomated);

        /// <summary>
        /// Used to report that playback has ended for an item
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        Task OnPlaybackStopped(PlaybackStopInfo info);

        /// <summary>
        /// Reports the session ended.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns>Task.</returns>
        void ReportSessionEnded(string sessionId);

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
        /// Sends the SyncPlayCommand.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendSyncPlayCommand(string sessionId, SendCommand command, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the SyncPlayGroupUpdate.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="command">The group update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
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
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The name.</param>
        /// <param name="data">The data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendMessageToAdminSessions<T>(string name, T data, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the message to user sessions.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>Task.</returns>
        Task SendMessageToUserSessions<T>(List<Guid> userIds, string name, T data, CancellationToken cancellationToken);

        Task SendMessageToUserSessions<T>(List<Guid> userIds, string name, Func<T> dataFn, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the message to user device sessions.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="name">The name.</param>
        /// <param name="data">The data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendMessageToUserDeviceSessions<T>(string deviceId, string name, T data, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the restart required message.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendRestartRequiredNotification(CancellationToken cancellationToken);

        /// <summary>
        /// Sends the server shutdown notification.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendServerShutdownNotification(CancellationToken cancellationToken);

        /// <summary>
        /// Sends the server restart notification.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendServerRestartNotification(CancellationToken cancellationToken);

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
        /// Reports the now viewing item.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="item">The item.</param>
        void ReportNowViewingItem(string sessionId, BaseItemDto item);

        /// <summary>
        /// Authenticates the new session.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{SessionInfo}.</returns>
        Task<AuthenticationResult> AuthenticateNewSession(AuthenticationRequest request);

        /// <summary>
        /// Creates the new session.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task&lt;AuthenticationResult&gt;.</returns>
        Task<AuthenticationResult> CreateNewSession(AuthenticationRequest request);

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
        /// Gets the session by authentication token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="remoteEndpoint">The remote endpoint.</param>
        /// <returns>SessionInfo.</returns>
        SessionInfo GetSessionByAuthenticationToken(string token, string deviceId, string remoteEndpoint);

        /// <summary>
        /// Gets the session by authentication token.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="remoteEndpoint">The remote endpoint.</param>
        /// <param name="appVersion">The application version.</param>
        /// <returns>Task&lt;SessionInfo&gt;.</returns>
        SessionInfo GetSessionByAuthenticationToken(AuthenticationInfo info, string deviceId, string remoteEndpoint, string appVersion);

        /// <summary>
        /// Logouts the specified access token.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <returns>Task.</returns>
        void Logout(string accessToken);
        void Logout(AuthenticationInfo accessToken);

        /// <summary>
        /// Revokes the user tokens.
        /// </summary>
        /// <returns>Task.</returns>
        void RevokeUserTokens(Guid userId, string currentAccessToken);

        /// <summary>
        /// Revokes the token.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Task.</returns>
        void RevokeToken(string id);

        void CloseIfNeeded(SessionInfo session);
    }
}
