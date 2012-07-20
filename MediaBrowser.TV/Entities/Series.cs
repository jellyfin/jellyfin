using MediaBrowser.Model.Entities;

namespace MediaBrowser.TV.Entities
{
    public class Series : Folder
    {
        public string TvdbId { get; set; }
        public string Status { get; set; }
    }
}
