
namespace MediaBrowser.Model.Configuration
{
    public class CinemaModeConfiguration
    {
        public bool EnableIntrosForMovies { get; set; }
        public bool EnableIntrosForEpisodes { get; set; }
        public bool EnableIntrosForWatchedContent { get; set; }
        public bool EnableIntrosFromUpcomingTrailers { get; set; }
        public bool EnableIntrosFromMoviesInLibrary { get; set; }
        public bool EnableIntrosParentalControl { get; set; }
        public bool EnableIntrosFromSimilarMovies { get; set; }
        public string CustomIntroPath { get; set; }
        public string MediaInfoIntroPath { get; set; }
        public bool EnableIntrosFromUpcomingDvdMovies { get; set; }
        public bool EnableIntrosFromUpcomingStreamingMovies { get; set; }

        public int TrailerLimit { get; set; }
        
        public CinemaModeConfiguration()
        {
            EnableIntrosParentalControl = true;
            EnableIntrosFromSimilarMovies = true;
            TrailerLimit = 2;
        }
    }
}
