using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace MediaBrowser.Model.Entities;

/// <summary>
/// Class ProviderIdsExtensions.
/// </summary>
public static partial class ProviderIdsExtensions
{
    /// <summary>
    /// Case-insensitive dictionary of <see cref="MetadataProvider"/> string representation.
    /// </summary>
    private static readonly Dictionary<string, string> _metadataProviderEnumDictionary =
        Enum.GetValues<MetadataProvider>()
            .ToDictionary(
                enumValue => enumValue.ToString(),
                enumValue => enumValue.ToString(),
                StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The known id formats, keyed by provider name.
    /// </summary>
    private static readonly Dictionary<string, Func<string, bool>> _providerIdValidators =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [MetadataProvider.Imdb.ToString()] = value => ImdbIdRegex().IsMatch(value),
            [MetadataProvider.Tmdb.ToString()] = IsPositiveNumber,
            [MetadataProvider.TmdbCollection.ToString()] = IsPositiveNumber,
            [MetadataProvider.AudioDbArtist.ToString()] = IsPositiveNumber,
            [MetadataProvider.AudioDbAlbum.ToString()] = IsPositiveNumber,

            // Every MusicBrainz id is an MBID.
            [MetadataProvider.MusicBrainzAlbum.ToString()] = IsGuid,
            [MetadataProvider.MusicBrainzAlbumArtist.ToString()] = IsGuid,
            [MetadataProvider.MusicBrainzArtist.ToString()] = IsGuid,
            [MetadataProvider.MusicBrainzReleaseGroup.ToString()] = IsGuid,
            [MetadataProvider.MusicBrainzRecording.ToString()] = IsGuid,
            [MetadataProvider.MusicBrainzTrack.ToString()] = IsGuid
        };

    /// <summary>
    /// Checks if this instance has an id for the given provider.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <param name="name">The of the provider name.</param>
    /// <returns><c>true</c> if a provider id with the given name was found; otherwise <c>false</c>.</returns>
    public static bool HasProviderId(this IHasProviderIds instance, string name)
        => instance.TryGetProviderId(name, out _);

    /// <summary>
    /// Checks if this instance has an id for the given provider.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <param name="provider">The provider.</param>
    /// <returns><c>true</c> if a provider id with the given name was found; otherwise <c>false</c>.</returns>
    public static bool HasProviderId(this IHasProviderIds instance, MetadataProvider provider)
        => instance.HasProviderId(provider.ToString());

    /// <summary>
    /// Gets a provider id.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <param name="name">The name.</param>
    /// <param name="id">The provider id.</param>
    /// <returns><c>true</c> if a provider id with the given name was found; otherwise <c>false</c>.</returns>
    public static bool TryGetProviderId(this IHasProviderIds instance, string name, [NotNullWhen(true)] out string? id)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (instance.ProviderIds is null)
        {
            id = null;
            return false;
        }

        var foundProviderId = instance.ProviderIds.TryGetValue(name, out id);
        // This occurs when searching with Identify (and possibly in other places)
        if (string.IsNullOrEmpty(id))
        {
            id = null;
            foundProviderId = false;
        }

        return foundProviderId;
    }

    /// <summary>
    /// Gets a provider id.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <param name="provider">The provider.</param>
    /// <param name="id">The provider id.</param>
    /// <returns><c>true</c> if a provider id with the given name was found; otherwise <c>false</c>.</returns>
    public static bool TryGetProviderId(this IHasProviderIds instance, MetadataProvider provider, [NotNullWhen(true)] out string? id)
    {
        return instance.TryGetProviderId(provider.ToString(), out id);
    }

    /// <summary>
    /// Gets a provider id.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <param name="name">The name.</param>
    /// <returns>System.String.</returns>
    public static string? GetProviderId(this IHasProviderIds instance, string name)
    {
        instance.TryGetProviderId(name, out string? id);
        return id;
    }

    /// <summary>
    /// Gets a provider id.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <param name="provider">The provider.</param>
    /// <returns>System.String.</returns>
    public static string? GetProviderId(this IHasProviderIds instance, MetadataProvider provider)
    {
        return instance.GetProviderId(provider.ToString());
    }

    /// <summary>
    /// Checks whether a value can be an id of the given provider.
    /// </summary>
    /// <param name="name">The provider name.</param>
    /// <param name="value">The provider id.</param>
    /// <returns><c>true</c> if the value has a plausible format for the provider; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// Providers regularly hand out an id belonging to a different service, e.g. an IMDb person id in the
    /// TMDb field. Such an id is not just useless, it also makes the owning provider fail for the item.
    /// </remarks>
    public static bool IsValidProviderId(string? name, string? value)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return !_providerIdValidators.TryGetValue(name, out var isValid) || isValid(value);
    }

    /// <summary>
    /// Sets a provider id.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <param name="name">The name, this should not contain a '=' character.</param>
    /// <param name="value">The value.</param>
    /// <remarks>Due to how deserialization from the database works the name cannot contain '='.</remarks>
    /// <returns><c>true</c> if the provider id got set successfully; otherwise, <c>false</c>.</returns>
    public static bool TrySetProviderId(this IHasProviderIds instance, string? name, string? value)
    {
        ArgumentNullException.ThrowIfNull(instance);

        // When name contains a '=' it can't be deserialized from the database
        if (string.IsNullOrWhiteSpace(name)
            || string.IsNullOrWhiteSpace(value)
            || name.Contains('=', StringComparison.Ordinal))
        {
            return false;
        }

        name = name.Trim();
        value = value.Trim();

        if (!IsValidProviderId(name, value))
        {
            return false;
        }

        // Ensure it exists
        instance.ProviderIds ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Match on internal MetadataProvider enum string values before adding arbitrary providers
        if (_metadataProviderEnumDictionary.TryGetValue(name, out var enumValue))
        {
            instance.ProviderIds[enumValue] = value;
        }
        else
        {
            instance.ProviderIds[name] = value;
        }

        return true;
    }

    /// <summary>
    /// Sets a provider id.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <param name="provider">The provider.</param>
    /// <param name="value">The value.</param>
    /// <returns><c>true</c> if the provider id got set successfully; otherwise, <c>false</c>.</returns>
    public static bool TrySetProviderId(this IHasProviderIds instance, MetadataProvider provider, string? value)
        => instance.TrySetProviderId(provider.ToString(), value);

    /// <summary>
    /// Sets a provider id.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <param name="name">The name, this should not contain a '=' character.</param>
    /// <param name="value">The value.</param>
    public static void SetProviderId(this IHasProviderIds instance, string name, string value)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        // When name contains a '=' it can't be deserialized from the database
        if (name.Contains('=', StringComparison.Ordinal))
        {
            throw new ArgumentException("Provider id name cannot contain '='", nameof(name));
        }

        instance.TrySetProviderId(name, value);
    }

    /// <summary>
    /// Replaces all provider ids, dropping the ones that cannot belong to the provider they are filed under.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <param name="providerIds">The provider ids to set.</param>
    public static void SetProviderIds(this IHasProviderIds instance, IReadOnlyDictionary<string, string>? providerIds)
    {
        ArgumentNullException.ThrowIfNull(instance);

        instance.ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (providerIds is null)
        {
            return;
        }

        foreach (var (name, value) in providerIds)
        {
            instance.TrySetProviderId(name, value);
        }
    }

    /// <summary>
    /// Sets a provider id.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <param name="provider">The provider.</param>
    /// <param name="value">The value.</param>
    public static void SetProviderId(this IHasProviderIds instance, MetadataProvider provider, string value)
        => instance.SetProviderId(provider.ToString(), value);

    /// <summary>
    /// Checks whether a provider gives both the same id, which says they are the same entity.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <param name="providerIds">The ids to compare against.</param>
    /// <returns><c>true</c> if a provider they both know agrees.</returns>
    public static bool SharesProviderId(this IHasProviderIds instance, IReadOnlyDictionary<string, string>? providerIds)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (providerIds is null || instance.ProviderIds is null)
        {
            return false;
        }

        foreach (var (provider, value) in providerIds)
        {
            if (!string.IsNullOrWhiteSpace(value)
                && string.Equals(instance.GetProviderId(provider), value, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the provider whose id says the two are different entities, or <c>null</c> when they can
    /// be the same.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <param name="providerIds">The ids to compare against.</param>
    /// <returns>The name of the disagreeing provider, or <c>null</c>.</returns>
    /// <remarks>
    /// Agreeing anywhere wins: one provider reusing an id is likelier than two entities sharing a
    /// name and an id.
    /// </remarks>
    public static string? FindConflictingProvider(this IHasProviderIds instance, IReadOnlyDictionary<string, string>? providerIds)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (providerIds is null || instance.ProviderIds is null)
        {
            return null;
        }

        string? conflict = null;
        foreach (var provider in providerIds.Keys.Order(StringComparer.Ordinal))
        {
            var value = providerIds[provider];
            var known = instance.GetProviderId(provider);
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(known))
            {
                continue;
            }

            if (string.Equals(known, value, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            conflict ??= provider;
        }

        return conflict;
    }

    /// <summary>
    /// Removes a provider id.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <param name="name">The name.</param>
    public static void RemoveProviderId(this IHasProviderIds instance, string name)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentException.ThrowIfNullOrEmpty(name);

        instance.ProviderIds?.Remove(name);
    }

    /// <summary>
    /// Removes a provider id.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <param name="provider">The provider.</param>
    public static void RemoveProviderId(this IHasProviderIds instance, MetadataProvider provider)
    {
        ArgumentNullException.ThrowIfNull(instance);

        instance.ProviderIds?.Remove(provider.ToString());
    }

    private static bool IsPositiveNumber(string value)
        => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0;

    private static bool IsGuid(string value)
        => Guid.TryParse(value, CultureInfo.InvariantCulture, out _);

    // An IMDb id is a type prefix (tt for titles, nm for people, co for companies, ...) followed by
    // digits. The prefix is optional because a bare number has always been accepted for a title.
    [GeneratedRegex(@"^(tt|nm|co|ev|ch|ni)?[0-9]+$", RegexOptions.IgnoreCase)]
    private static partial Regex ImdbIdRegex();
}
