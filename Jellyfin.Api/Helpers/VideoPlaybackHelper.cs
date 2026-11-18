using System;
using System.Linq;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Results;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Resolves authenticated video requests to the shared playback registry.
/// </summary>
public sealed class VideoPlaybackHelper
{
    private readonly object _legacyGate = new();
    private readonly IVideoPlaybackCoordinator _coordinator;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoPlaybackHelper"/> class.
    /// </summary>
    /// <param name="coordinator">The playback registry.</param>
    /// <param name="userManager">The user manager.</param>
    /// <param name="libraryManager">The library manager.</param>
    public VideoPlaybackHelper(IVideoPlaybackCoordinator coordinator, IUserManager userManager, ILibraryManager libraryManager)
    {
        _coordinator = coordinator;
        _userManager = userManager;
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Registers playback metadata without reserving a playback slot.
    /// </summary>
    /// <param name="context">The authenticated request.</param>
    /// <param name="itemId">The canonical library item identifier.</param>
    /// <param name="playSessionId">The server-generated encoding identifier.</param>
    /// <param name="playbackInstanceId">The stable client identifier, or null for legacy clients.</param>
    /// <returns>The resolved logical identifier, or null for requests outside user video playback.</returns>
    public string? RegisterPlayback(HttpContext context, Guid itemId, string playSessionId, string? playbackInstanceId)
    {
        var playback = GetContext(context, itemId, playbackInstanceId);
        if (!playback.HasValue)
        {
            return null;
        }

        var status = _coordinator.RegisterPlaySession(playback.Value, playSessionId);
        if (status == VideoPlaybackStatus.Expired)
        {
            // Expiry retired the old logical epoch; this metadata request has a fresh server alias.
            status = _coordinator.RegisterPlaySession(playback.Value, playSessionId);
        }

        EnsureSuccess(status);
        return playback.Value.PlaybackInstanceId;
    }

    /// <summary>
    /// Acquires an operation before serving video bytes or starting an encoder.
    /// </summary>
    /// <param name="context">The authenticated request.</param>
    /// <param name="itemId">The canonical library item identifier.</param>
    /// <param name="playSessionId">The encoding identifier, if provided.</param>
    /// <returns>The operation handle, or null for HEAD, audio or application API keys.</returns>
    public IVideoPlaybackRequest? Acquire(HttpContext context, Guid itemId, string? playSessionId)
        => Acquire(context, itemId, playSessionId, out _);

    /// <summary>
    /// Acquires an operation and resolves the encoding identifier used by legacy requests.
    /// </summary>
    /// <param name="context">The authenticated request.</param>
    /// <param name="itemId">The canonical library item identifier.</param>
    /// <param name="playSessionId">The encoding identifier, if provided.</param>
    /// <param name="resolvedPlaySessionId">The identifier to propagate into URLs and encoding jobs.</param>
    /// <returns>The operation handle, or null for requests outside playback admission.</returns>
    public IVideoPlaybackRequest? Acquire(HttpContext context, Guid itemId, string? playSessionId, out string? resolvedPlaySessionId)
    {
        resolvedPlaySessionId = playSessionId;
        if (HttpMethods.IsHead(context.Request.Method))
        {
            return null;
        }

        var legacy = GetContext(context, itemId, null);
        if (!legacy.HasValue)
        {
            return null;
        }

        var playback = legacy.Value;
        VideoPlaybackStatus status;
        if (string.IsNullOrWhiteSpace(playSessionId))
        {
            // One alias per legacy epoch, rather than one entry for every parallel segment.
            lock (_legacyGate)
            {
                status = _coordinator.ResolvePlayback(playback, out playSessionId);
                if (status is VideoPlaybackStatus.NotFound or VideoPlaybackStatus.Expired)
                {
                    playSessionId = Guid.NewGuid().ToString("N");
                    status = _coordinator.RegisterPlaySession(playback, playSessionId);
                }
            }
        }
        else
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(playSessionId.Length, 128);
            status = _coordinator.ResolvePlaySession(playback.UserId, playback.SessionId, playback.ItemId, playSessionId, out var registered);
            if (status == VideoPlaybackStatus.Success)
            {
                playback = registered;
            }
            else if (status == VideoPlaybackStatus.NotFound)
            {
                // Older clients may generate an encoding identifier without requesting metadata.
                status = _coordinator.RegisterPlaySession(playback, playSessionId);
            }
        }

        EnsureSuccess(status);
        var user = _userManager.GetUserById(playback.UserId)
            ?? throw new SecurityException("The playback user no longer exists.");
        status = _coordinator.TryAcquire(playback, playSessionId!, user.MaxActiveVideoStreams, out var request);
        EnsureSuccess(status);
        resolvedPlaySessionId = playSessionId;
        return request;
    }

    /// <summary>
    /// Resolves the current legacy alias for an authenticated item report.
    /// </summary>
    /// <param name="context">The authenticated request.</param>
    /// <param name="itemId">The canonical library item identifier.</param>
    /// <returns>The current alias, or null when no legacy playback is registered.</returns>
    public string? ResolveLegacyPlaySession(HttpContext context, Guid itemId)
    {
        if (itemId.Equals(Guid.Empty))
        {
            return null;
        }

        var userId = context.User.GetUserId();
        var ownerId = context.User.GetPlaybackOwnerId();
        if (userId.Equals(Guid.Empty) || string.IsNullOrEmpty(ownerId))
        {
            return null;
        }

        var playback = new VideoPlaybackContext(userId, ownerId, itemId, VideoPlaybackContext.CreateLegacyId(userId, ownerId, itemId));
        return _coordinator.ResolvePlayback(playback, out var alias) == VideoPlaybackStatus.Success
            ? alias
            : null;
    }

    /// <summary>
    /// Keeps an operation alive while MVC executes the response body.
    /// </summary>
    /// <param name="result">The video or playlist response.</param>
    /// <param name="request">The action's operation, disposed by its caller.</param>
    /// <returns>A response owning an independent operation reference.</returns>
    public static ActionResult Wrap(ActionResult result, IVideoPlaybackRequest? request)
        => request is null ? result : new VideoPlaybackResult(result, request.Retain());

    /// <summary>
    /// Verifies encoding ownership without acquiring or releasing playback quota.
    /// </summary>
    /// <param name="context">The authenticated request.</param>
    /// <param name="playSessionId">The encoding identifier.</param>
    public void VerifyPlaySession(HttpContext context, string playSessionId)
    {
        var userId = context.User.GetUserId();
        if (userId.Equals(Guid.Empty) && context.User.GetIsApiKey())
        {
            return;
        }

        var ownerId = context.User.GetPlaybackOwnerId();
        if (string.IsNullOrEmpty(ownerId))
        {
            throw new SecurityException("A verified playback owner is required.");
        }

        EnsureSuccess(_coordinator.ResolvePlaySession(userId, ownerId, null, playSessionId, out _));
    }

    /// <summary>
    /// Verifies video ownership before an encoding control request has side effects.
    /// </summary>
    /// <param name="context">The authenticated request.</param>
    /// <param name="playSessionId">The encoding identifier.</param>
    /// <param name="job">The current encoding job, if present.</param>
    /// <param name="reportActivity">Whether this is a real client heartbeat.</param>
    public void VerifyEncodingControl(HttpContext context, string playSessionId, TranscodingJob? job, bool reportActivity = false)
    {
        var userId = context.User.GetUserId();
        if (userId.Equals(Guid.Empty) && context.User.GetIsApiKey())
        {
            return;
        }

        var ownerId = context.User.GetPlaybackOwnerId();
        if (string.IsNullOrEmpty(ownerId))
        {
            throw new SecurityException("A verified playback owner is required.");
        }

        var status = _coordinator.ResolvePlaySession(userId, ownerId, null, playSessionId, out var playback);
        if (status == VideoPlaybackStatus.Success)
        {
            if (reportActivity)
            {
                _coordinator.ReportActivity(playback, playSessionId);
            }

            return;
        }

        // Terminal aliases still retain verified ownership for encoder cleanup.
        // Audio jobs and absent jobs have no admission in the video registry.
        if (status is VideoPlaybackStatus.Stopped or VideoPlaybackStatus.Expired
            || (status == VideoPlaybackStatus.NotFound
                && (job is null || (job.MediaSource is not null && !job.MediaSource.MediaStreams.Any(stream => stream.Type == MediaStreamType.Video)))))
        {
            return;
        }

        EnsureSuccess(status);
    }

    private VideoPlaybackContext? GetContext(HttpContext context, Guid itemId, string? playbackInstanceId)
    {
        var userId = context.User.GetUserId();
        if (userId.Equals(Guid.Empty))
        {
            return null;
        }

        var ownerId = context.User.GetPlaybackOwnerId();
        if (string.IsNullOrEmpty(ownerId))
        {
            throw new SecurityException("A verified playback owner is required.");
        }

        var item = _libraryManager.GetItemById<BaseItem>(itemId, userId)
            ?? throw new ResourceNotFoundException("The playback item was not found.");
        if (item.MediaType != MediaType.Video)
        {
            return null;
        }

        if (playbackInstanceId is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(playbackInstanceId);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(playbackInstanceId.Length, 128);
        }

        return new VideoPlaybackContext(userId, ownerId, item.Id, playbackInstanceId ?? VideoPlaybackContext.CreateLegacyId(userId, ownerId, item.Id));
    }

    private static void EnsureSuccess(VideoPlaybackStatus status)
    {
        if (status == VideoPlaybackStatus.LimitReached)
        {
            throw new VideoPlaybackLimitException();
        }

        if (status != VideoPlaybackStatus.Success)
        {
            throw new SecurityException("The video playback authorization is invalid or no longer active.");
        }
    }
}
