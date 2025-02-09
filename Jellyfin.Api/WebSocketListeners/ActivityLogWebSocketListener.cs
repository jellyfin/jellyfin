using System;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Events;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.WebSocketListeners;

/// <summary>
/// Class ActivityLogWebSocketListener.
/// </summary>
public class ActivityLogWebSocketListener : BasePeriodicWebSocketListener<ActivityLogEntry[], WebSocketListenerState>
{
    /// <summary>
    /// The _kernel.
    /// </summary>
    private readonly IActivityManager _activityManager;

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityLogWebSocketListener"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{ActivityLogWebSocketListener}"/> interface.</param>
    /// <param name="activityManager">Instance of the <see cref="IActivityManager"/> interface.</param>
    public ActivityLogWebSocketListener(ILogger<ActivityLogWebSocketListener> logger, IActivityManager activityManager)
        : base(logger)
    {
        _activityManager = activityManager;
        _activityManager.EntryCreated += OnEntryCreated;
    }

    /// <inheritdoc />
    protected override SessionMessageType Type => SessionMessageType.ActivityLogEntry;

    /// <inheritdoc />
    protected override SessionMessageType StartType => SessionMessageType.ActivityLogEntryStart;

    /// <inheritdoc />
    protected override SessionMessageType StopType => SessionMessageType.ActivityLogEntryStop;

    /// <summary>
    /// Gets the data to send.
    /// </summary>
    /// <returns>Task{SystemInfo}.</returns>
    protected override Task<ActivityLogEntry[]> GetDataToSend()
    {
        return Task.FromResult(Array.Empty<ActivityLogEntry>());
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        if (!_disposed)
        {
            _activityManager.EntryCreated -= OnEntryCreated;
            _disposed = true;
        }

        await base.DisposeAsyncCore().ConfigureAwait(false);
    }

    /// <summary>
    /// Starts sending messages over an activity log web socket.
    /// </summary>
    /// <param name="message">The message.</param>
    protected override void Start(WebSocketMessageInfo message)
    {
        if (!message.Connection.AuthorizationInfo.User.HasPermission(PermissionKind.IsAdministrator) && !message.Connection.AuthorizationInfo.IsApiKey)
        {
            throw new AuthenticationException("Only admin users can retrieve the activity log.");
        }

        base.Start(message);
    }

    private void OnEntryCreated(object? sender, GenericEventArgs<ActivityLogEntry> e)
    {
        SendData(true);
    }
}
