
namespace MediaBrowser.Model.Querying
{
    /// <summary>
    /// Class ThemeMediaResult
    /// </summary>
    public class ThemeMediaResult : ItemsResult
    {
        /// <summary>
        /// Gets or sets the owner id.
        /// </summary>
        /// <value>The owner id.</value>
        public string OwnerId { get; set; }
    }

    public class AllThemeMediaResult
    {
        public ThemeMediaResult ThemeVideosResult { get; set; }

        public ThemeMediaResult ThemeSongsResult { get; set; }

        public ThemeMediaResult SoundtrackSongsResult { get; set; }
        
        public AllThemeMediaResult()
        {
            ThemeVideosResult = new ThemeMediaResult();

            ThemeSongsResult = new ThemeMediaResult();

            SoundtrackSongsResult = new ThemeMediaResult();
        }
    }
}
