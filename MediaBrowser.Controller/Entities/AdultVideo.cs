
namespace MediaBrowser.Controller.Entities
{
    public class AdultVideo : Video, IHasPreferredMetadataLanguage
    {
        public string PreferredMetadataLanguage { get; set; }
    }
}
