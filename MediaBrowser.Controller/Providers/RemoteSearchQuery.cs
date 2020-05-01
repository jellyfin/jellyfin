using System;

namespace MediaBrowser.Controller.Providers
{
    public class RemoteSearchQuery<T>
        where T : ItemLookupInfo
    {
        public T SearchInfo { get; set; }

        public Guid ItemId { get; set; }

        /// <summary>
        /// Will only search within the given provider when set.
        /// </summary>
        public string SearchProviderName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether disabled providers should be included.
        /// </summary>
        /// <value><c>true</c> if disabled providers should be included.</value>
        public bool IncludeDisabledProviders { get; set; }
    }
}
