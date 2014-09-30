
namespace MediaBrowser.Model.Configuration
{
    public class CinemaModeConfiguration
    {
        public bool EnableIntrosForMovies { get; set; }
        public bool EnableIntrosForEpisodes { get; set; }
        public bool EnableIntrosForWatchedContent { get; set; }
        public bool EnableIntrosFromUpcomingTrailers { get; set; }
        public bool EnableIntrosFromMoviesInLibrary { get; set; }
        public bool EnableCustomIntro { get; set; }
        public bool EnableIntrosParentalControl { get; set; }
        public string CustomIntroPath { get; set; }
        public bool EnableIntrosFromUpcomingDvdMovies { get; set; }
        public bool EnableIntrosFromUpcomingStreamingMovies { get; set; }

        public CinemaModeConfiguration()
        {
            EnableCustomIntro = true;
            EnableIntrosFromMoviesInLibrary = true;
            EnableIntrosFromUpcomingTrailers = true;
            EnableIntrosParentalControl = true;
            EnableIntrosFromUpcomingDvdMovies = true;
            EnableIntrosFromUpcomingStreamingMovies = true;
        }
    }
}
