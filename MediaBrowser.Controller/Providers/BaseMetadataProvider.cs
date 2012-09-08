using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    public abstract class BaseMetadataProvider
    {
        public abstract bool Supports(BaseEntity item);

        public virtual bool RequiresInternet
        {
            get
            {
                return false;
            }
        }

        public abstract Task FetchAsync(BaseEntity item, ItemResolveEventArgs args);

        public abstract MetadataProviderPriority Priority { get; }
    }

    /// <summary>
    /// Determines when a provider should execute, relative to others
    /// </summary>
    public enum MetadataProviderPriority
    {
        // Run this provider at the beginning
        First = 1,

        // Run this provider after all first priority providers
        Second = 2,

        // Run this provider after all second priority providers
        Third = 3,

        // Run this provider last
        Last = 4
    }
}
