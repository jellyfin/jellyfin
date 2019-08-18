namespace MediaBrowser.Providers.Tmdb.Models.Movies
{
    public class Cast
    {
        public int id { get; set; }
        public string name { get; set; }
        public string character { get; set; }
        public int order { get; set; }
        public int cast_id { get; set; }
        public string profile_path { get; set; }
    }
}
