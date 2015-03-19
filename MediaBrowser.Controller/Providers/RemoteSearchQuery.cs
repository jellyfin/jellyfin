namespace MediaBrowser.Controller.Providers
{
    public class RemoteSearchQuery<T>
        where T : ItemLookupInfo
    {
        public T SearchInfo { get; set; }

        /// <summary>
        /// If set will only search within the given provider
        /// </summary>
        public string SearchProviderName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [include disabled providers].
        /// </summary>
        /// <value><c>true</c> if [include disabled providers]; otherwise, <c>false</c>.</value>
        public bool IncludeDisabledProviders { get; set; }
    }
}