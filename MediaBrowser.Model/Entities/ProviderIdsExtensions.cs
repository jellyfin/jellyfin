using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Class ProviderIdsExtensions.
    /// </summary>
    public static class ProviderIdsExtensions
    {
        /// <summary>
        /// Gets a provider id.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="name">The name.</param>
        /// <param name="id">The provider id.</param>
        /// <returns><c>true</c> if a provider id with the given name was found; otherwise <c>false</c>.</returns>
        public static bool TryGetProviderId(this IHasProviderIds instance, string name, [MaybeNullWhen(false)] out string id)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (instance.ProviderIds == null)
            {
                id = null;
                return false;
            }

            return instance.ProviderIds.TryGetValue(name, out id);
        }

        /// <summary>
        /// Gets a provider id.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="provider">The provider.</param>
        /// <param name="id">The provider id.</param>
        /// <returns><c>true</c> if a provider id with the given name was found; otherwise <c>false</c>.</returns>
        public static bool TryGetProviderId(this IHasProviderIds instance, MetadataProvider provider, [MaybeNullWhen(false)] out string id)
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
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public static void SetProviderId(this IHasProviderIds instance, string name, string value)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            // If it's null remove the key from the dictionary
            if (string.IsNullOrEmpty(value))
            {
                instance.ProviderIds?.Remove(name);
            }
            else
            {
                // Ensure it exists
                if (instance.ProviderIds == null)
                {
                    instance.ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }

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
        {
            instance.SetProviderId(provider.ToString(), value);
        }
    }
}
