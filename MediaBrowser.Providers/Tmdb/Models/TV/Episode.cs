namespace MediaBrowser.Providers.Tmdb.Models.TV
{
    public class Episode
    {
        public string Air_Date { get; set; }
        public int Episode_Number { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Overview { get; set; }
        public string Still_Path { get; set; }
        public double Vote_Average { get; set; }
        public int Vote_Count { get; set; }
    }
}
