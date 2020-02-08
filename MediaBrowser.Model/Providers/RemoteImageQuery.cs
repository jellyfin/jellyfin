#pragma warning disable CS1591
#pragma warning disable SA1600

using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Providers
{
    public class RemoteImageQuery
    {
        public string ProviderName { get; set; }

        public ImageType? ImageType { get; set; }

        public bool IncludeDisabledProviders { get; set; }

        public bool IncludeAllLanguages { get; set; }
    }
}
