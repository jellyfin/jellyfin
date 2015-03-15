namespace MediaBrowser.Model.LiveTv
{
    public class LiveTvOptions
    {
        public int? GuideDays { get; set; }
        public bool EnableMovieProviders { get; set; }

        public LiveTvOptions()
        {
            EnableMovieProviders = true;
        }
    }
}