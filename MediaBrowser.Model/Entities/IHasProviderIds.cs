using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Since BaseItem and DTOBaseItem both have ProviderIds, this interface helps avoid code repition by using extension methods
    /// </summary>
    public interface IHasProviderIds
    {
        /// <summary>
        /// Gets or sets the provider ids.
        /// </summary>
        /// <value>The provider ids.</value>
        Dictionary<string, string> ProviderIds { get; set; }
    }

    /// <summary>
    /// Class ProviderIdsExtensions
    /// </summary>
    public static class ProviderIdsExtensions
    {
        /// <summary>
        /// Determines whether [has provider identifier] [the specified instance].
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="provider">The provider.</param>
        /// <returns><c>true</c> if [has provider identifier] [the specified instance]; otherwise, <c>false</c>.</returns>
        public static bool HasProviderId(this IHasProviderIds instance, MetadataProviders provider)
        {
            return !string.IsNullOrEmpty(instance.GetProviderId(provider.ToString()));
        }
        
        /// <summary>
        /// Gets a provider id
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="provider">The provider.</param>
        /// <returns>System.String.</returns>
        public static string GetProviderId(this IHasProviderIds instance, MetadataProviders provider)
        {
            return instance.GetProviderId(provider.ToString());
        }

        /// <summary>
        /// Gets a provider id
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="name">The name.</param>
        /// <returns>System.String.</returns>
        public static string GetProviderId(this IHasProviderIds instance, string name)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            if (instance.ProviderIds == null)
            {
                return null;
            }

            string id;
            instance.ProviderIds.TryGetValue(name, out id);
            return id;
        }

        /// <summary>
        /// Sets a provider id
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public static void SetProviderId(this IHasProviderIds instance, string name, string value)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }
            
            // If it's null remove the key from the dictionary
            if (string.IsNullOrEmpty(value))
            {
                if (instance.ProviderIds != null)
                {
                    if (instance.ProviderIds.ContainsKey(name))
                    {
                        instance.ProviderIds.Remove(name);
                    }
                }
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
        /// Sets a provider id
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="provider">The provider.</param>
        /// <param name="value">The value.</param>
        public static void SetProviderId(this IHasProviderIds instance, MetadataProviders provider, string value)
        {
            instance.SetProviderId(provider.ToString(), value);
        }
    }
}
