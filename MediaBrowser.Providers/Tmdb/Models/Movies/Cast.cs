namespace MediaBrowser.Providers.Tmdb.Models.Movies
{
    public class Cast
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Character { get; set; }
        public int Order { get; set; }
        public int Cast_Id { get; set; }
        public string Profile_Path { get; set; }
    }
}
