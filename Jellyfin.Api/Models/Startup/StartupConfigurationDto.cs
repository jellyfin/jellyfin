namespace Jellyfin.Api.Models.Startup
{
    public class StartupConfigurationDto
    {
        public string UICulture { get; set; }
        public string MetadataCountryCode { get; set; }
        public string PreferredMetadataLanguage { get; set; }
    }
}
