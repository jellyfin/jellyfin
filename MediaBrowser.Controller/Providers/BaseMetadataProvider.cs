using System.Threading.Tasks;
using MediaBrowser.Controller.Events;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Providers
{
    public abstract class BaseMetadataProvider
    {
        /// <summary>
        /// If the provider needs any startup routines, add them here
        /// </summary>
        public virtual void Init()
        {
        }

        public virtual bool Supports(BaseItem item)
        {
            return true;
        }

        public abstract Task Fetch(BaseItem item, ItemResolveEventArgs args);
    }
}
