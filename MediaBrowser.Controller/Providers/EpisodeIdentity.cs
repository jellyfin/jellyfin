namespace MediaBrowser.Controller.Providers
{
    public class EpisodeIdentity : IItemIdentity
    {
        public string Type { get; set; }

        public string SeriesId { get; set; }
        public int? SeasonIndex { get; set; }
        public int IndexNumber { get; set; }
        public int? IndexNumberEnd { get; set; }
    }
}