#pragma warning disable CS1591

using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Providers
{
    public class RemoteImageQuery
    {
        public RemoteImageQuery(string providerName)
        {
            ProviderName = providerName;
        }

        public string ProviderName { get; }

        public ImageType? ImageType { get; set; }

        public bool IncludeDisabledProviders { get; set; }

        public bool IncludeAllLanguages { get; set; }
    }
}
