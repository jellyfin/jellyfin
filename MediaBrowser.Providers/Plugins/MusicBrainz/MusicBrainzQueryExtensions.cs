using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MetaBrainz.Common;
using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Plugins.MusicBrainz;

/// <summary>
/// Helpers for talking to MusicBrainz with identifiers that are not guaranteed to be valid.
/// </summary>
internal static class MusicBrainzQueryExtensions
{
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
            () => query.LookupReleaseAsync(releaseId, include, cancellationToken),
            "release",
            releaseId,
            logger);

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
            () => query.LookupReleaseGroupAsync(releaseGroupId, include, null, cancellationToken),
            "release group",
            releaseGroupId,
            logger);

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
            () => query.LookupArtistAsync(artistId, include, null, null, cancellationToken),
            "artist",
            artistId,
            logger);

    /// <summary>
    /// Runs a lookup, mapping a "not found" response to <see langword="null"/>. Identifiers stored on a library item
    /// can refer to entities that no longer exist in MusicBrainz, which is not an error worth failing a refresh over.
    /// </summary>
    /// <typeparam name="T">The type of entity being looked up.</typeparam>
    /// <param name="lookup">The lookup to run.</param>
    /// <param name="entityType">The type of entity being looked up, used for logging.</param>
    /// <param name="id">The identifier being looked up, used for logging.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The entity, or <see langword="null"/> if MusicBrainz does not have it.</returns>
    private static async Task<T?> NotFoundAsNullAsync<T>(Func<Task<T>> lookup, string entityType, Guid id, ILogger logger)
        where T : class
    {
        try
        {
            return await lookup().ConfigureAwait(false);
        }
        catch (HttpError ex) when (ex.Status == HttpStatusCode.NotFound)
        {
            logger.LogDebug("MusicBrainz has no {EntityType} with id {Id}", entityType, id);
            return null;
        }
    }
}
