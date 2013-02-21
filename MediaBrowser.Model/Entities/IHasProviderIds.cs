using System.Collections.Generic;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Since BaseItem and DTOBaseItem both have ProviderIds, this interface helps avoid code repition by using extension methods
    /// </summary>
    public interface IHasProviderIds
    {
        Dictionary<string, string> ProviderIds { get; set; }
    }

    public static class ProviderIdsExtensions
    {
        /// <summary>
        /// Gets a provider id
        /// </summary>
        public static string GetProviderId(this IHasProviderIds instance, MetadataProviders provider)
        {
            return instance.GetProviderId(provider.ToString());
        }

        /// <summary>
        /// Gets a provider id
        /// </summary>
        public static string GetProviderId(this IHasProviderIds instance, string name)
        {
            if (instance.ProviderIds == null)
            {
                return null;
            }

            return instance.ProviderIds[name];
        }

        /// <summary>
        /// Sets a provider id
        /// </summary>
        public static void SetProviderId(this IHasProviderIds instance, string name, string value)
        {
            if (instance.ProviderIds == null)
            {
                instance.ProviderIds = new Dictionary<string, string>();
            }

            instance.ProviderIds[name] = value;
        }

        /// <summary>
        /// Sets a provider id
        /// </summary>
        public static void SetProviderId(this IHasProviderIds instance, MetadataProviders provider, string value)
        {
            instance.SetProviderId(provider.ToString(), value);
        }
    }
}
