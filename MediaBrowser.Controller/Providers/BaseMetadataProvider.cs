using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Common.Extensions;
using System.Threading.Tasks;
using System;

namespace MediaBrowser.Controller.Providers
{
    public abstract class BaseMetadataProvider
    {
        protected Guid _id;
        public virtual Guid Id
        {
            get
            {
                if (_id == null) _id = this.GetType().FullName.GetMD5();
                return _id;
            }
        }

        public abstract bool Supports(BaseEntity item);

        public virtual bool RequiresInternet
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the last refresh time of this provider for this item. Providers that care should
        /// call SetLastRefreshed to update this value.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected virtual DateTime LastRefreshed(BaseEntity item)
        {
            return (item.ProviderData.GetValueOrDefault(this.Id, new BaseProviderInfo())).LastRefreshed;
        }

        /// <summary>
        /// Sets the persisted last refresh date on the item for this provider.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="value"></param>
        protected virtual void SetLastRefreshed(BaseEntity item, DateTime value)
        {
            var data = item.ProviderData.GetValueOrDefault(this.Id, new BaseProviderInfo());
            data.LastRefreshed = value;
            item.ProviderData[this.Id] = data;
        }

        /// <summary>
        /// Returns whether or not this provider should be re-fetched.  Default functionality can
        /// compare a provided date with a last refresh time.  This can be overridden for more complex
        /// determinations.
        /// </summary>
        /// <returns></returns>
        public virtual bool NeedsRefresh(BaseEntity item)
        {
            return CompareDate(item) > LastRefreshed(item);
        }

        /// <summary>
        /// Override this to return the date that should be compared to the last refresh date
        /// to determine if this provider should be re-fetched.
        /// </summary>
        protected virtual DateTime CompareDate(BaseEntity item)
        {
            return DateTime.MinValue.AddMinutes(1); // want this to be greater than mindate so new items will refresh
        }

        public virtual Task FetchIfNeededAsync(BaseEntity item)
        {
            if (this.NeedsRefresh(item))
                return FetchAsync(item, item.ResolveArgs);
            else
                return new Task(() => { });
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
