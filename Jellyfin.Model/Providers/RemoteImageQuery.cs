using Jellyfin.Model.Entities;

namespace Jellyfin.Model.Providers
{
    public class RemoteImageQuery
    {
        public string ProviderName { get; set; }

        public ImageType? ImageType { get; set; }

        public bool IncludeDisabledProviders { get; set; }

        public bool IncludeAllLanguages { get; set; }
    }
}
