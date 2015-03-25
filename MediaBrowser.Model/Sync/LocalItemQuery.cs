
namespace MediaBrowser.Model.Sync
{
    public class LocalItemQuery
    {
        public string ServerId { get; set; }
        public string AlbumArtistId { get; set; }
        public string AlbumId { get; set; }
        public string SeriesId { get; set; }
        public string Type { get; set; }
        public string MediaType { get; set; }
        public string[] ExcludeTypes { get; set; }

        public LocalItemQuery()
        {
            ExcludeTypes = new string[] { };
        }
    }
}
