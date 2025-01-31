using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MediaBrowser.Model.Entities;

/// <summary>
/// Class ProviderIdsExtensions.
/// </summary>
public static class ProviderIdsExtensions
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
    /// <remarks>Due to how deserialization from the database works the name cannot contain '='.</remarks>
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
}
