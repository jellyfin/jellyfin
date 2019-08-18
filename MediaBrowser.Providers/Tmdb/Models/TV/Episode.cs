namespace MediaBrowser.Providers.Tmdb.Models.TV
{
    public class Episode
    {
        public string air_date { get; set; }
        public int episode_number { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public string overview { get; set; }
        public string still_path { get; set; }
        public double vote_average { get; set; }
        public int vote_count { get; set; }
    }
}
