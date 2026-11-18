using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Controller.MediaEncoding;

namespace MediaBrowser.Controller.Session;

/// <summary>
/// Associates opened live stream consumers with their authenticated playback owners.
/// </summary>
public interface ILiveStreamPlaybackManager
{
    /// <summary>
    /// Registers one consumer created by a successful provider open operation.
    /// </summary>
    /// <param name="playback">The verified logical playback.</param>
    /// <param name="playSessionId">The input consumer's encoding identifier.</param>
    /// <param name="liveStreamId">The opened live stream identifier.</param>
    /// <param name="sessionId">The server session used for legacy cleanup.</param>
    /// <param name="isVideo">Whether the consumer participates in video playback expiry.</param>
    /// <returns>False for an equivalent registration; the caller must release its duplicate provider consumer.</returns>
    bool Register(VideoPlaybackContext playback, string playSessionId, string liveStreamId, string sessionId, bool isVideo = true);

    /// <summary>
    /// Finds an existing consumer to make a repeated open operation idempotent.
    /// </summary>
    /// <param name="userId">The authenticated user.</param>
    /// <param name="ownerId">The authenticated token record identifier.</param>
    /// <param name="itemId">The canonical library item.</param>
    /// <param name="playSessionId">The encoding identifier.</param>
    /// <param name="liveStreamId">The opened live stream when found.</param>
    /// <returns>Whether the caller owns a consumer whose resource still exists.</returns>
    bool TryGetLiveStream(Guid userId, string ownerId, Guid itemId, string playSessionId, out string? liveStreamId);

    /// <summary>
    /// Verifies that a source belongs to the authenticated item and playback.
    /// </summary>
    /// <param name="userId">The authenticated user.</param>
    /// <param name="ownerId">The authenticated token record identifier.</param>
    /// <param name="itemId">The canonical library item.</param>
    /// <param name="liveStreamId">The opened resource.</param>
    /// <param name="playSessionId">The current encoding identifier, if supplied.</param>
    /// <param name="isVideoOutput">Whether the caller will return video bytes, rather than metadata or extracted audio.</param>
    /// <returns>Whether the source can be used by this playback.</returns>
    bool Verify(Guid userId, string ownerId, Guid itemId, string liveStreamId, string? playSessionId = null, bool isVideoOutput = true);

    /// <summary>
    /// Verifies a control request owner without treating a missing item as media access.
    /// </summary>
    /// <param name="userId">The authenticated user.</param>
    /// <param name="ownerId">The authenticated token record identifier.</param>
    /// <param name="liveStreamId">The opened or recently closed resource.</param>
    /// <param name="hasOpenConsumer">Whether control may proceed, rather than acknowledge an already closed consumer.</param>
    /// <returns>Whether this owner has an open consumer or a recent idempotent close.</returns>
    bool VerifyOwner(Guid userId, string ownerId, string liveStreamId, out bool hasOpenConsumer);

    /// <summary>
    /// Tests whether an encoding identifier still owns an input consumer.
    /// </summary>
    /// <param name="playSessionId">The encoding identifier.</param>
    /// <returns>Whether a consumer is registered.</returns>
    bool ContainsPlaySession(string playSessionId);

    /// <summary>
    /// Releases every input consumer belonging to one verified logical playback.
    /// </summary>
    /// <param name="liveStreamId">The opened resource.</param>
    /// <param name="playback">The verified playback that stopped.</param>
    /// <returns>Whether this is a managed resource, including already released consumers.</returns>
    /// <remarks>
    /// The video stop must already have been accepted by the coordinator.
    /// A new live epoch with the same legacy context remains untouched.
    /// </remarks>
    Task<bool> ClosePlaybackAsync(string liveStreamId, VideoPlaybackContext playback);

    /// <summary>
    /// Handles legacy server cleanup without releasing another playback's consumer.
    /// </summary>
    /// <param name="liveStreamId">The opened resource.</param>
    /// <param name="sessionIdOrPlaySessionId">The server session or input encoding identifier.</param>
    /// <returns>True for a managed resource even when there is no matching consumer to release.</returns>
    Task<bool> CloseSessionAsync(string liveStreamId, string sessionIdOrPlaySessionId);

    /// <summary>
    /// Closes a source consumer requested by its authenticated owner.
    /// </summary>
    /// <param name="userId">The authenticated user.</param>
    /// <param name="ownerId">The authenticated token record identifier.</param>
    /// <param name="liveStreamId">The opened resource.</param>
    /// <param name="playSessionId">The input encoding identifier, if supplied.</param>
    /// <returns>The asynchronous close operation.</returns>
    Task CloseOwnedAsync(Guid userId, string ownerId, string liveStreamId, string? playSessionId = null);

    /// <summary>
    /// Takes a snapshot of encoding identifiers still referenced by input consumers.
    /// </summary>
    /// <returns>Identifiers to preserve during alias cleanup.</returns>
    IReadOnlySet<string> GetProtectedPlaySessions();

    /// <summary>
    /// Releases expired video consumers and removes ownership for closed resources.
    /// </summary>
    /// <returns>The asynchronous cleanup operation.</returns>
    Task RemoveExpiredAsync();

    /// <summary>
    /// Cleans consumers while preserving inputs still used by server audio encoding jobs.
    /// </summary>
    /// <param name="jobs">A snapshot of the current server encoding jobs.</param>
    /// <returns>The asynchronous cleanup operation.</returns>
    Task RemoveExpiredAsync(IReadOnlyCollection<TranscodingJob> jobs);
}
