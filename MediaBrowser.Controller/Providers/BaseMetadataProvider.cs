using System;
using System.Threading.Tasks;
using MediaBrowser.Controller.Events;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Providers
{
    public abstract class BaseMetadataProvider : IDisposable
    {
        /// <summary>
        /// If the provider needs any startup routines, add them here
        /// </summary>
        public virtual void Init()
        {
        }

        /// <summary>
        /// Disposes anything created during Init
        /// </summary>
        public virtual void Dispose()
        {
        }

        public abstract bool Supports(BaseEntity item);

        public virtual bool RequiresInternet
        {
            get
            {
                return false;
            }
        }

        public abstract Task Fetch(BaseEntity item, ItemResolveEventArgs args);
    }
}
