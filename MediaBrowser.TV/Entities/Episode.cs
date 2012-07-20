using MediaBrowser.Model.Entities;

namespace MediaBrowser.TV.Entities
{
    public class Episode : Video
    {
        public string SeasonNumber { get; set; }
    }
}
