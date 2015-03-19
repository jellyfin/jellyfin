namespace MediaBrowser.Controller.Providers
{
    public class SeasonIdentity : IItemIdentity
    {
        public string Type { get; set; }

        public string SeriesId { get; set; }

        public int SeasonIndex { get; set; }
    }
}