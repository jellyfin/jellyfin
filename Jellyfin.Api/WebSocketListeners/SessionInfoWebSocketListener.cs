using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.WebSocketListeners;

/// <summary>
/// Class SessionInfoWebSocketListener.
/// </summary>
public class SessionInfoWebSocketListener : BasePeriodicWebSocketListener<IEnumerable<SessionInfoDto>, WebSocketListenerState>
{
    private readonly ISessionManager _sessionManager;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionInfoWebSocketListener"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{SessionInfoWebSocketListener}"/> interface.</param>
    /// <param name="sessionManager">Instance of the <see cref="ISessionManager"/> interface.</param>
    public SessionInfoWebSocketListener(ILogger<SessionInfoWebSocketListener> logger, ISessionManager sessionManager)
        : base(logger)
    {
        _sessionManager = sessionManager;

        _sessionManager.SessionStarted += OnSessionManagerSessionStarted;
        _sessionManager.SessionEnded += OnSessionManagerSessionEnded;
        _sessionManager.PlaybackStart += OnSessionManagerPlaybackStart;
        _sessionManager.PlaybackStopped += OnSessionManagerPlaybackStopped;
        _sessionManager.PlaybackProgress += OnSessionManagerPlaybackProgress;
        _sessionManager.CapabilitiesChanged += OnSessionManagerCapabilitiesChanged;
        _sessionManager.SessionActivity += OnSessionManagerSessionActivity;
    }

    /// <inheritdoc />
    protected override SessionMessageType Type => SessionMessageType.Sessions;

    /// <inheritdoc />
    protected override SessionMessageType StartType => SessionMessageType.SessionsStart;

    /// <inheritdoc />
    protected override SessionMessageType StopType => SessionMessageType.SessionsStop;

    /// <summary>
    /// Gets the data to send.
    /// </summary>
    /// <returns>Task{SystemInfo}.</returns>
    protected override Task<IEnumerable<SessionInfoDto>> GetDataToSend()
    {
        return Task.FromResult(_sessionManager.Sessions.Select(_sessionManager.ToSessionInfoDto));
    }

    /// <inheritdoc />
    protected override Task<IEnumerable<SessionInfoDto>> GetDataToSendForConnection(IWebSocketConnection connection)
    {
        var sessions = _sessionManager.Sessions;

        // For non-admin users, filter the sessions to only include their own sessions
        if (connection.AuthorizationInfo?.User is not null &&
            !connection.AuthorizationInfo.IsApiKey &&
            !connection.AuthorizationInfo.User.HasPermission(PermissionKind.IsAdministrator))
        {
            var userId = connection.AuthorizationInfo.User.Id;
            sessions = sessions.Where(s => s.UserId.Equals(userId) || s.ContainsUser(userId));
        }

        return Task.FromResult(sessions.Select(_sessionManager.ToSessionInfoDto));
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        if (!_disposed)
        {
            _sessionManager.SessionStarted -= OnSessionManagerSessionStarted;
            _sessionManager.SessionEnded -= OnSessionManagerSessionEnded;
            _sessionManager.PlaybackStart -= OnSessionManagerPlaybackStart;
            _sessionManager.PlaybackStopped -= OnSessionManagerPlaybackStopped;
            _sessionManager.PlaybackProgress -= OnSessionManagerPlaybackProgress;
            _sessionManager.CapabilitiesChanged -= OnSessionManagerCapabilitiesChanged;
            _sessionManager.SessionActivity -= OnSessionManagerSessionActivity;
            _disposed = true;
        }

        await base.DisposeAsyncCore().ConfigureAwait(false);
    }

    /// <summary>
    /// Starts sending messages over a session info web socket.
    /// </summary>
    /// <param name="message">The message.</param>
    protected override void Start(WebSocketMessageInfo message)
    {
        // Allow all authenticated users to subscribe to session information
        if (message.Connection.AuthorizationInfo.User is null && !message.Connection.AuthorizationInfo.IsApiKey)
        {
            throw new AuthenticationException("User must be authenticated to subscribe to session Information.");
        }

        base.Start(message);
    }

    private void OnSessionManagerSessionActivity(object? sender, SessionEventArgs e)
    {
        SendData(false);
    }

    private void OnSessionManagerCapabilitiesChanged(object? sender, SessionEventArgs e)
    {
        SendData(true);
    }

    private void OnSessionManagerPlaybackProgress(object? sender, PlaybackProgressEventArgs e)
    {
        SendData(!e.IsAutomated);
    }

    private void OnSessionManagerPlaybackStopped(object? sender, PlaybackStopEventArgs e)
    {
        SendData(true);
    }

    private void OnSessionManagerPlaybackStart(object? sender, PlaybackProgressEventArgs e)
    {
        SendData(true);
    }

    private void OnSessionManagerSessionEnded(object? sender, SessionEventArgs e)
    {
        SendData(true);
    }

    private void OnSessionManagerSessionStarted(object? sender, SessionEventArgs e)
    {
        SendData(true);
    }
}
