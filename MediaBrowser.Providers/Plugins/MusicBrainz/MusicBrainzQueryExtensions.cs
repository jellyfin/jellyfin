using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MetaBrainz.Common;
using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using MetaBrainz.MusicBrainz.Interfaces.Searches;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Plugins.MusicBrainz;

/// <summary>
/// Helpers for talking to MusicBrainz with identifiers that are not guaranteed to be valid.
/// </summary>
internal static class MusicBrainzQueryExtensions
{
    /// <summary>
    /// The number of extra attempts made when MusicBrainz reports a transient failure.
    /// </summary>
    private const int MaxRetries = 2;

    private static readonly TimeSpan _minimumRetryDelay = TimeSpan.FromSeconds(1);

    private static readonly TimeSpan _maximumRetryDelay = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Parses a MusicBrainz identifier, which may come from user-supplied tags or NFO files and is therefore not
    /// guaranteed to be a valid GUID.
    /// </summary>
    /// <param name="id">The identifier to parse.</param>
    /// <param name="entityType">The type of entity the identifier refers to, used for logging.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The parsed identifier, or <see langword="null"/> if it is missing or malformed.</returns>
    public static Guid? ParseMusicBrainzId(string? id, string entityType, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        if (!Guid.TryParse(id, out var parsedId))
        {
            logger.LogDebug("Ignoring malformed MusicBrainz {EntityType} id {Id}", entityType, id);
            return null;
        }

        return parsedId;
    }

    /// <summary>
    /// Looks up a release, treating an unknown identifier as missing data rather than an error.
    /// </summary>
    /// <param name="query">The MusicBrainz query client.</param>
    /// <param name="releaseId">The release identifier.</param>
    /// <param name="include">The additional data to include in the lookup.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The release, or <see langword="null"/> if MusicBrainz does not have it.</returns>
    public static Task<IRelease?> LookupReleaseOrNullAsync(this Query query, Guid releaseId, Include include, ILogger logger, CancellationToken cancellationToken)
        => NotFoundAsNullAsync(
            token => query.LookupReleaseAsync(releaseId, include, token),
            "release",
            releaseId,
            logger,
            cancellationToken);

    /// <summary>
    /// Looks up a release group, treating an unknown identifier as missing data rather than an error.
    /// </summary>
    /// <param name="query">The MusicBrainz query client.</param>
    /// <param name="releaseGroupId">The release group identifier.</param>
    /// <param name="include">The additional data to include in the lookup.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The release group, or <see langword="null"/> if MusicBrainz does not have it.</returns>
    public static Task<IReleaseGroup?> LookupReleaseGroupOrNullAsync(this Query query, Guid releaseGroupId, Include include, ILogger logger, CancellationToken cancellationToken)
        => NotFoundAsNullAsync(
            token => query.LookupReleaseGroupAsync(releaseGroupId, include, null, token),
            "release group",
            releaseGroupId,
            logger,
            cancellationToken);

    /// <summary>
    /// Looks up an artist, treating an unknown identifier as missing data rather than an error.
    /// </summary>
    /// <param name="query">The MusicBrainz query client.</param>
    /// <param name="artistId">The artist identifier.</param>
    /// <param name="include">The additional data to include in the lookup.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The artist, or <see langword="null"/> if MusicBrainz does not have it.</returns>
    public static Task<IArtist?> LookupArtistOrNullAsync(this Query query, Guid artistId, Include include, ILogger logger, CancellationToken cancellationToken)
        => NotFoundAsNullAsync(
            token => query.LookupArtistAsync(artistId, include, null, null, token),
            "artist",
            artistId,
            logger,
            cancellationToken);

    /// <summary>
    /// Searches for artists, retrying when the MusicBrainz server is too busy to answer.
    /// </summary>
    /// <param name="query">The MusicBrainz query client.</param>
    /// <param name="searchQuery">The search query.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The search results.</returns>
    public static Task<ISearchResults<ISearchResult<IArtist>>> FindArtistsWithRetryAsync(this Query query, string searchQuery, ILogger logger, CancellationToken cancellationToken)
        => RetryOnTransientErrorAsync(
            token => query.FindArtistsAsync(searchQuery, null, null, false, token),
            "artist search",
            logger,
            cancellationToken);

    /// <summary>
    /// Searches for releases, retrying when the MusicBrainz server is too busy to answer.
    /// </summary>
    /// <param name="query">The MusicBrainz query client.</param>
    /// <param name="searchQuery">The search query.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The search results.</returns>
    public static Task<ISearchResults<ISearchResult<IRelease>>> FindReleasesWithRetryAsync(this Query query, string searchQuery, ILogger logger, CancellationToken cancellationToken)
        => RetryOnTransientErrorAsync(
            token => query.FindReleasesAsync(searchQuery, null, null, false, token),
            "release search",
            logger,
            cancellationToken);

    /// <summary>
    /// Runs a request, retrying it when MusicBrainz reports a transient failure. MusicBrainz sheds load with
    /// HTTP 503 when its servers are busy, which is not specific to this client and succeeds when retried, so
    /// failing the whole refresh on the first one would leave items without metadata for no good reason.
    /// </summary>
    /// <typeparam name="T">The type of the request result.</typeparam>
    /// <param name="request">The request to run.</param>
    /// <param name="operation">The operation being performed, used for logging.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The request result.</returns>
    internal static async Task<T> RetryOnTransientErrorAsync<T>(Func<CancellationToken, Task<T>> request, string operation, ILogger logger, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await request(cancellationToken).ConfigureAwait(false);
            }
            catch (HttpError ex) when (attempt <= MaxRetries && IsTransient(ex.Status))
            {
                var delay = GetRetryDelay(ex, attempt);
                logger.LogDebug(
                    ex,
                    "MusicBrainz {Operation} failed with {Status}, retrying in {Delay} (attempt {Attempt} of {Attempts})",
                    operation,
                    ex.Status,
                    delay,
                    attempt,
                    MaxRetries + 1);

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Determines whether a response status is worth retrying. These are all cases of the server being unable to
    /// answer right now rather than of the request itself being wrong.
    /// </summary>
    /// <param name="status">The status returned by MusicBrainz.</param>
    /// <returns>Whether the request should be retried.</returns>
    private static bool IsTransient(HttpStatusCode status)
        => status is HttpStatusCode.TooManyRequests
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;

    /// <summary>
    /// Works out how long to wait before retrying. MusicBrainz reports when its current rate limit window ends and
    /// retrying before then is documented to fail, so that hint wins over the exponential backoff when it is longer.
    /// </summary>
    /// <param name="error">The error returned by MusicBrainz.</param>
    /// <param name="attempt">The number of the attempt that just failed.</param>
    /// <returns>The time to wait before the next attempt.</returns>
    internal static TimeSpan GetRetryDelay(HttpError error, int attempt)
    {
        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
        var hint = GetServerHint(error);
        if (hint > delay)
        {
            delay = hint.Value;
        }

        if (delay < _minimumRetryDelay)
        {
            return _minimumRetryDelay;
        }

        return delay > _maximumRetryDelay ? _maximumRetryDelay : delay;
    }

    private static TimeSpan? GetServerHint(HttpError error)
    {
        var headers = error.ResponseHeaders;
        if (headers is null)
        {
            return null;
        }

        var retryAfter = headers.RetryAfter;
        if (retryAfter?.Delta is { } delta)
        {
            return delta;
        }

        if (retryAfter?.Date is { } date)
        {
            return date - DateTimeOffset.UtcNow;
        }

        var rateLimit = new RateLimitInfo(headers);
        if (rateLimit.ResetIn is { } resetIn)
        {
            return TimeSpan.FromSeconds(resetIn);
        }

        return rateLimit.ResetAt - rateLimit.LastRequest;
    }

    /// <summary>
    /// Runs a lookup, mapping a "not found" response to <see langword="null"/>. Identifiers stored on a library item
    /// can refer to entities that no longer exist in MusicBrainz, which is not an error worth failing a refresh over.
    /// </summary>
    /// <typeparam name="T">The type of entity being looked up.</typeparam>
    /// <param name="lookup">The lookup to run.</param>
    /// <param name="entityType">The type of entity being looked up, used for logging.</param>
    /// <param name="id">The identifier being looked up, used for logging.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The entity, or <see langword="null"/> if MusicBrainz does not have it.</returns>
    private static async Task<T?> NotFoundAsNullAsync<T>(Func<CancellationToken, Task<T>> lookup, string entityType, Guid id, ILogger logger, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            return await RetryOnTransientErrorAsync(lookup, entityType + " lookup", logger, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpError ex) when (ex.Status == HttpStatusCode.NotFound)
        {
            logger.LogDebug("MusicBrainz has no {EntityType} with id {Id}", entityType, id);
            return null;
        }
    }
}
