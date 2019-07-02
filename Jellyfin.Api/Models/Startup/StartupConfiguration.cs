namespace Jellyfin.Api.Models.Startup
{
    public class StartupConfiguration
    {
        public string UICulture { get; set; }
        public string MetadataCountryCode { get; set; }
        public string PreferredMetadataLanguage { get; set; }
    }
}
